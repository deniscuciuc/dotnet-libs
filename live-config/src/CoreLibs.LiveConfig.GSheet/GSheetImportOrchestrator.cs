using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;
using CoreLibs.LiveConfig.GSheet.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.GSheet;

public sealed class GSheetImportOrchestrator(
    ILogger<GSheetImportOrchestrator> logger,
    IServiceProvider services,
    GSheetParserService parserService,
    GSheetImportContext context,
    GSheetOptions options,
    IEnumerable<IGSheetPipelineObserver> observers)
{
    private readonly IGSheetPipelineObserver[] _observers = observers.ToArray();

    private readonly List<List<Func<IServiceProvider, Task>>> _executionPlan =
        BuildExecutionPlan(parserService, logger, context, options);

    private readonly List<(string SheetName, string Range)> _allSheetRanges = CollectAllSheetRanges();

    public async Task RunSpecificImporterAsync<TImporter>(CancellationToken cancellationToken = default)
        where TImporter : class
    {
        var importerType = typeof(TImporter);
        logger.LogInformation("Running specific GSheet importer: {ImporterName}", importerType.Name);
        var sw = Stopwatch.StartNew();
        context.Clear();

        // Validate the type has either [GSheetImporter] or entity registration
        var importerAttr = importerType.GetCustomAttribute<GSheetImporterAttribute>();
        var entityReg = GSheetImporterRegistry.GetStaticEntityRegistrations()
            .FirstOrDefault(r => r.AdapterType == importerType);
        if (importerAttr is null && entityReg is null)
            throw new InvalidOperationException(
                $"Importer {importerType.Name} does not have [GSheetImporter] attribute or entity registration.");

        var allImporterTypes = GSheetImporterRegistry.DiscoverImporterTypes().ToList();
        foreach (var reg in GSheetImporterRegistry.GetStaticEntityRegistrations())
            if (!allImporterTypes.Contains(reg.AdapterType))
                allImporterTypes.Add(reg.AdapterType);

        var importersToRun = CollectImporterWithDependencies(importerType, allImporterTypes);

        var sheetRanges = new List<(string SheetName, string Range)>();
        foreach (var t in importersToRun)
        {
            var attr = t.GetCustomAttribute<GSheetImporterAttribute>();
            if (attr is not null)
            {
                sheetRanges.Add((attr.SheetName, attr.Range));
                continue;
            }

            var reg = GSheetImporterRegistry.GetStaticEntityRegistrations()
                .FirstOrDefault(r => r.AdapterType == t);
            if (reg is not null)
                sheetRanges.Add((reg.SheetName, reg.Range));
        }

        sheetRanges = sheetRanges.Distinct().ToList();

        logger.LogInformation("Preloading {Count} sheets for importer {ImporterName} and its dependencies",
            sheetRanges.Count, importerType.Name);

        try
        {
            await parserService.PreloadAllSheetsAsync(sheetRanges, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to preload sheets for importer {ImporterName}", importerType.Name);
            if (options.FailOnImportErrors)
                throw CreatePreloadException(sheetRanges, ex);
            throw;
        }

        var nodes = BuildNodes(importersToRun);
        var sortedLevels = TopologicallySort(nodes);

        foreach (var node in sortedLevels.SelectMany(levelNodes => levelNodes))
        {
            var (rowType, domainType) = GetImporterGenericArgs(node.Type);
            var executor = BuildExecutor(node, rowType, domainType, parserService, logger, context, options,
                cancellationToken);
            await executor(services);
        }

        sw.Stop();
        logger.LogInformation("Finished specific GSheet importer {ImporterName} in {ElapsedMs} ms", importerType.Name,
            sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Runs a specific importer by name (class name, sheet name, or fuzzy match).
    /// </summary>
    public async Task RunSpecificImporterByNameAsync(string importerName, CancellationToken cancellationToken = default)
    {
        var importerType = GSheetImporterRegistry.ResolveImporterType(importerName)
                           ?? throw new InvalidOperationException($"Importer '{importerName}' not found.");

        var method = typeof(GSheetImportOrchestrator)
            .GetMethod(nameof(RunSpecificImporterAsync), BindingFlags.Public | BindingFlags.Instance)!;
        var generic = method.MakeGenericMethod(importerType);
        await (Task)generic.Invoke(this, [cancellationToken])!;
    }

    /// <summary>
    /// Runs multiple importers by name in a single batched operation.
    /// All sheet ranges for all requested importers (and their dependencies) are fetched
    /// in a single Google Sheets batch request, then importers are executed in dependency order.
    /// </summary>
    public async Task RunMultipleImportersByNameAsync(
        IReadOnlyCollection<string> importerNames, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        context.Clear();

        var allImporterTypes = GSheetImporterRegistry.DiscoverImporterTypes().ToList();
        foreach (var reg in GSheetImporterRegistry.GetStaticEntityRegistrations())
            if (!allImporterTypes.Contains(reg.AdapterType))
                allImporterTypes.Add(reg.AdapterType);

        // Resolve all requested importers + their full dependency trees
        var allImportersToRun = new HashSet<Type>();
        foreach (var name in importerNames)
        {
            var importerType = GSheetImporterRegistry.ResolveImporterType(name)
                               ?? throw new InvalidOperationException($"Importer '{name}' not found.");
            foreach (var dep in CollectImporterWithDependencies(importerType, allImporterTypes))
                allImportersToRun.Add(dep);
        }

        // Collect all distinct sheet ranges
        var sheetRanges = new List<(string SheetName, string Range)>();
        foreach (var t in allImportersToRun)
        {
            var attr = t.GetCustomAttribute<GSheetImporterAttribute>();
            if (attr is not null)
            {
                sheetRanges.Add((attr.SheetName, attr.Range));
                continue;
            }

            var reg = GSheetImporterRegistry.GetStaticEntityRegistrations()
                .FirstOrDefault(r => r.AdapterType == t);
            if (reg is not null)
                sheetRanges.Add((reg.SheetName, reg.Range));
        }

        sheetRanges = sheetRanges.Distinct().ToList();

        logger.LogInformation(
            "Preloading {RangeCount} sheet ranges for {ImporterCount} importers in a single batch request",
            sheetRanges.Count, allImportersToRun.Count);

        try
        {
            await parserService.PreloadAllSheetsAsync(sheetRanges, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to preload sheets for batch import");
            if (options.FailOnImportErrors)
                throw CreatePreloadException(sheetRanges, ex);
            throw;
        }

        // Execute importers in topological (dependency) order
        var nodes = BuildNodes(allImportersToRun);
        var sortedLevels = TopologicallySort(nodes);

        foreach (var node in sortedLevels.SelectMany(levelNodes => levelNodes))
        {
            var (rowType, domainType) = GetImporterGenericArgs(node.Type);
            var executor = BuildExecutor(node, rowType, domainType, parserService, logger, context, options,
                cancellationToken);
            await executor(services);
        }

        sw.Stop();
        logger.LogInformation(
            "Finished batch import of {Count} importers in {ElapsedMs} ms",
            allImportersToRun.Count, sw.ElapsedMilliseconds);
    }

    private static List<Type> CollectImporterWithDependencies(Type importerType, List<Type> allImporterTypes)
    {
        var result = new HashSet<Type>();
        var queue = new Queue<Type>();
        queue.Enqueue(importerType);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current))
                continue;

            // Get dependencies from attribute or entity registration
            Type[]? needs = null;
            var attr = current.GetCustomAttribute<GSheetImporterAttribute>();
            if (attr?.Needs != null)
            {
                needs = attr.Needs;
            }
            else
            {
                var entityReg = GSheetImporterRegistry.GetStaticEntityRegistrations()
                    .FirstOrDefault(r => r.AdapterType == current);
                needs = entityReg?.Needs;
            }

            if (needs is null) continue;

            foreach (var dep in needs)
            {
                var depType = allImporterTypes.FirstOrDefault(t => t == dep);
                if (depType != null && !result.Contains(depType))
                    queue.Enqueue(depType);
            }
        }

        return result.ToList();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var totalSteps = _executionPlan.Sum(batch => batch.Count);
        logger.LogInformation("GSheet import run started. Steps: {Count}, Batches: {Batches}", totalSteps,
            _executionPlan.Count);
        var swAll = Stopwatch.StartNew();
        context.Clear();

        logger.LogInformation("Preloading {Count} sheets in a single batch request", _allSheetRanges.Count);

        try
        {
            await parserService.PreloadAllSheetsAsync(_allSheetRanges, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to preload sheets");
            if (options.FailOnImportErrors)
                throw new GSheetPreloadException(
                    _allSheetRanges.Select(x => new GSheetImportTarget(x.SheetName, x.Range)).ToArray(),
                    ex);
            throw;
        }

        for (var i = 0; i < _executionPlan.Count; i++)
        {
            var batch = _executionPlan[i];
            logger.LogInformation("Executing batch {BatchIndex}/{TotalBatches} with {Count} importers in parallel",
                i + 1, _executionPlan.Count, batch.Count);

            var tasks = batch.Select(step => step(services)).ToArray();
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                var failures = tasks
                    .Where(task => task.IsFaulted)
                    .SelectMany<Task, Exception>(task => task.Exception is null
                        ? Array.Empty<Exception>()
                        : task.Exception.Flatten().InnerExceptions)
                    .ToList();

                foreach (var failure in failures)
                    logger.LogError(failure, "Batch import failure: {Message}", failure.Message);

                switch (failures.Count)
                {
                    case 1:
                        ExceptionDispatchInfo.Capture(failures[0]).Throw();
                        break;
                    case > 1:
                        throw new GSheetImportBatchException(failures);
                }

                throw;
            }
        }

        swAll.Stop();
        logger.LogInformation("Finished GSheet import orchestrator in {ElapsedMs} ms", swAll.ElapsedMilliseconds);
    }

    /// <summary>
    /// Runs the full pipeline in dry-run mode: parses, validates, and maps all importers
    /// but does not commit domain data or call ProcessAsync/ImportAsync.
    /// Returns the aggregated import report.
    /// </summary>
    public async Task<GSheetImportReport> PreviewAsync(CancellationToken cancellationToken = default)
    {
        var originalDryRun = options.DryRun;
        try
        {
            options.DryRun = true;
            await RunAsync(cancellationToken);
            return context.Report;
        }
        finally
        {
            options.DryRun = originalDryRun;
        }
    }

    private static List<(string SheetName, string Range)> CollectAllSheetRanges()
    {
        var ranges = GSheetImporterRegistry.DiscoverImporterTypes()
            .Select(t => t.GetCustomAttribute<GSheetImporterAttribute>())
            .Where(attr => attr != null)
            .Select(attr => (attr!.SheetName, attr.Range))
            .ToList();

        ranges.AddRange(GSheetImporterRegistry.GetStaticEntityRegistrations()
            .Select(reg => (reg.SheetName, reg.Range)));

        return ranges.Distinct().ToList();
    }

    private static List<List<Func<IServiceProvider, Task>>> BuildExecutionPlan(
        GSheetParserService parserService,
        ILogger<GSheetImportOrchestrator> logger,
        GSheetImportContext context,
        GSheetOptions options)
    {
        var importerTypes = GSheetImporterRegistry.DiscoverImporterTypes().ToList();

        // Add entity adapter types from static registrations
        foreach (var reg in GSheetImporterRegistry.GetStaticEntityRegistrations())
            if (!importerTypes.Contains(reg.AdapterType))
                importerTypes.Add(reg.AdapterType);

        logger.LogInformation("Discovered {Count} GSheet importers/entities.", importerTypes.Count);
        var nodes = BuildNodes(importerTypes);
        var sortedLevels = TopologicallySort(nodes);

        var plan = new List<List<Func<IServiceProvider, Task>>>(sortedLevels.Count);
        foreach (var levelNodes in sortedLevels)
        {
            var batch = new List<Func<IServiceProvider, Task>>(levelNodes.Count);
            foreach (var node in levelNodes)
            {
                var (rowType, domainType) = GetImporterGenericArgs(node.Type);
                var exec = BuildExecutor(node, rowType, domainType, parserService, logger, context, options,
                    CancellationToken.None);
                batch.Add(exec);
            }

            plan.Add(batch);
        }

        return plan;
    }

    private static Func<IServiceProvider, Task> BuildExecutor(
        ImporterNode node,
        Type rowType,
        Type domainType,
        GSheetParserService parserService,
        ILogger<GSheetImportOrchestrator> logger,
        GSheetImportContext context,
        GSheetOptions options,
        CancellationToken cancellationToken)
    {
        var isPipeline = GSheetImporterRegistry.IsPipelineType(node.Type);

        var methodName = isPipeline
            ? nameof(ExecutePipelineAsync)
            : nameof(ExecuteImporterAsync);

        var method = typeof(GSheetImportOrchestrator)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(rowType, domainType);
        return sp =>
        {
            // Try DI first, then fallback to static entity adapter
            var importer = sp.GetService(node.Type)
                           ?? GSheetImporterRegistry.GetEntityAdapter(node.Type)
                           ?? throw new InvalidOperationException(
                               $"Could not resolve importer '{node.Type.Name}' from DI or entity registry.");
            if (!isPipeline)
                return (Task)generic.Invoke(null,
                [
                    importer, parserService, node.SheetName, node.Range, logger, context, options, cancellationToken
                ])!;

            var obs = sp.GetServices<IGSheetPipelineObserver>().ToArray();
            return (Task)generic.Invoke(null,
            [
                importer, parserService, node.SheetName, node.Range, logger, context, options, obs,
                cancellationToken
            ])!;
        };
    }

    private static async Task ExecuteImporterAsync<TRow, TDomain>(
        IGSheetImporter<TRow, TDomain> importer,
        GSheetParserService parser,
        string sheet,
        string range,
        ILogger logger,
        GSheetImportContext context,
        GSheetOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Importer {Importer}: START - parsing sheet '{Sheet}' range '{Range}'",
                importer.GetType().Name, sheet, range);

            context.SheetName = sheet;

            var mapper = importer.CreateMapper();
            var rows = await parser.ParseAsync(sheet, range, mapper, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Importer {Importer}: parsed {RowCount} rows", importer.GetType().Name, rows.Count);
            context.SetRows(rows);

            var validationResult = importer.ValidateRows(rows);

            if (validationResult.HasErrors)
            {
                logger.LogWarning("Importer {Importer}: validation found {ErrorCount} errors",
                    importer.GetType().Name, validationResult.Errors.Count);

                foreach (var error in validationResult.Errors)
                    logger.LogWarning("Importer {Importer}: Validation Error - {Error}", importer.GetType().Name,
                        error);

                if (options.FailOnValidationErrors)
                    throw GSheetImportException.ValidationFailure(
                        importer.GetType().Name, sheet, range, validationResult.Errors);

                var invalidRowIndices = new HashSet<int>(validationResult.Errors.Select(e => e.RowIndex));
                var validRows = rows.Where((_, index) => !invalidRowIndices.Contains(index)).ToList();

                logger.LogInformation(
                    "Importer {Importer}: continuing with {ValidCount} valid rows (skipped {InvalidCount})",
                    importer.GetType().Name, validRows.Count, invalidRowIndices.Count);

                if (validRows.Count == 0)
                {
                    logger.LogWarning("Importer {Importer}: no valid rows to import, skipping.",
                        importer.GetType().Name);
                    return;
                }

                rows = validRows;
            }

            var domains = importer.MapToDomain(rows, context);
            var domainList = domains is IList<TDomain> list ? list : domains.ToList();
            logger.LogInformation("Importer {Importer}: mapped {Count} domain entities", importer.GetType().Name,
                domainList.Count);
            context.SetDomain(domainList);

            var ok = await importer.ImportAsync(domainList, context).ConfigureAwait(false);

            if (ok)
            {
                logger.LogInformation("Importer {Importer}: COMPLETED - import successful", importer.GetType().Name);
            }
            else
            {
                logger.LogWarning("Importer {Importer}: COMPLETED - import reported failure", importer.GetType().Name);
                if (options.FailOnImportErrors)
                    throw GSheetImportException.ProcessingFailure(
                        importer.GetType().Name, sheet, range,
                        new InvalidOperationException($"Import failed for importer {importer.GetType().Name}"));
            }
        }
        catch (GSheetImportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Importer {Importer}: error occurred during import", importer.GetType().Name);
            if (options.FailOnImportErrors)
            {
                if (ex is GSheetPreloadException or GSheetImportBatchException)
                    throw;

                throw GSheetImportException.ProcessingFailure(
                    importer.GetType().Name, sheet, range, ex);
            }
        }
    }

    /// <summary>
    /// Executes a pipeline-based importer (IGSheetPipeline) through the full staged pipeline:
    /// structural validation → parse → attribute validation → custom row validation →
    /// transformer → graph validation → reference resolution → aggregator/MapToDomain → ProcessAsync.
    /// </summary>
    private static async Task ExecutePipelineAsync<TRow, TDomain>(
        IGSheetPipeline<TRow, TDomain> pipeline,
        GSheetParserService parser,
        string sheet,
        string range,
        ILogger logger,
        GSheetImportContext context,
        GSheetOptions options,
        IGSheetPipelineObserver[] observers,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var pipelineName = pipeline.GetType().Name;
        var allErrors = new List<GSheetValidationError>();
        var allWarnings = new List<GSheetWarning>();

        try
        {
            logger.LogInformation("Pipeline {Pipeline}: START - sheet '{Sheet}' range '{Range}'",
                pipelineName, sheet, range);

            // Track current sheet for richer error reporting
            context.SheetName = sheet;

            foreach (var obs in observers) obs.OnBeforeParse(pipelineName, sheet, range);

            // 1. Resolve mapper: custom or auto-generated from attributes
            var mapper = pipeline.CreateMapper() ?? AttributeClassMapBuilder.Build<TRow>();

            // 2. Parse rows from cached sheet data
            var rows = await parser.ParseAsync(sheet, range, mapper, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Pipeline {Pipeline}: parsed {RowCount} rows", pipelineName, rows.Count);
            context.SetRows(rows);

            foreach (var obs in observers) obs.OnAfterParse(pipelineName, rows.Count);

            foreach (var obs in observers) obs.OnBeforeValidation(pipelineName, rows.Count);

            // 3. Attribute-based row validation (automatic from [GSheetRequired], [GSheetRange], [GSheetRegex])
            var attrValidation = AttributeRowValidator.Validate(rows);
            if (attrValidation.HasErrors)
            {
                logger.LogWarning("Pipeline {Pipeline}: attribute validation found {ErrorCount} errors",
                    pipelineName, attrValidation.Errors.Count);
                allErrors.AddRange(attrValidation.Errors);

                if (options.ImportMode == GSheetImportMode.Strict)
                    throw GSheetImportException.ValidationFailure(pipelineName, sheet, range, attrValidation.Errors);
            }

            // 4. Custom row validation
            var rowValidator = pipeline.RowValidator;
            if (rowValidator is not null)
            {
                var rowValidation = rowValidator.Validate(rows, context);
                if (rowValidation.HasErrors)
                {
                    logger.LogWarning("Pipeline {Pipeline}: custom row validation found {ErrorCount} errors",
                        pipelineName, rowValidation.Errors.Count);
                    allErrors.AddRange(rowValidation.Errors);

                    if (options.ImportMode == GSheetImportMode.Strict)
                        throw GSheetImportException.ValidationFailure(pipelineName, sheet, range,
                            GSheetValidationResult.Merge(attrValidation, rowValidation).Errors);
                }
            }

            // 5. Filter out invalid rows when SkipInvalid
            if (allErrors.Count > 0 && options.ImportMode == GSheetImportMode.SkipInvalid)
            {
                var invalidRowIndices = new HashSet<int>(allErrors.Where(e => e.RowIndex >= 0).Select(e => e.RowIndex));
                if (invalidRowIndices.Count > 0)
                {
                    var filteredRows = rows.Where((_, index) => !invalidRowIndices.Contains(index)).ToList();
                    logger.LogInformation(
                        "Pipeline {Pipeline}: continuing with {ValidCount} valid rows (skipped {InvalidCount})",
                        pipelineName, filteredRows.Count, invalidRowIndices.Count);

                    if (filteredRows.Count == 0)
                    {
                        logger.LogWarning("Pipeline {Pipeline}: no valid rows to import, skipping.", pipelineName);
                        RecordReport(context, pipelineName, sheet, range, rows.Count, 0, allErrors, allWarnings, sw,
                            true);
                        return;
                    }

                    rows = filteredRows;
                }
            }

            // 6. Transformer
            var transformer = pipeline.Transformer;
            if (transformer is not null)
            {
                rows = transformer.Transform(rows, context);
                logger.LogInformation("Pipeline {Pipeline}: transformed → {RowCount} rows", pipelineName, rows.Count);
            }

            // 7. Graph validation
            var graphValidator = pipeline.GraphValidator;
            if (graphValidator is not null)
            {
                var graphValidation = graphValidator.Validate(rows, context);
                if (graphValidation.HasErrors)
                {
                    logger.LogWarning("Pipeline {Pipeline}: graph validation found {ErrorCount} errors",
                        pipelineName, graphValidation.Errors.Count);
                    allErrors.AddRange(graphValidation.Errors);

                    if (options.ImportMode == GSheetImportMode.Strict)
                        throw GSheetImportException.ValidationFailure(pipelineName, sheet, range, allErrors);
                }
            }

            foreach (var obs in observers) obs.OnAfterValidation(pipelineName, allErrors.Count);

            // 8. Reference resolution
            var refValidation = ReferenceResolver.Validate(rows, context, sheet);
            if (refValidation.HasErrors)
            {
                logger.LogWarning("Pipeline {Pipeline}: reference validation found {ErrorCount} errors",
                    pipelineName, refValidation.Errors.Count);
                allErrors.AddRange(refValidation.Errors);

                if (options.ImportMode == GSheetImportMode.Strict)
                    throw GSheetImportException.ValidationFailure(pipelineName, sheet, range, allErrors);
            }

            foreach (var obs in observers) obs.OnBeforeMapping(pipelineName, rows.Count);

            // 9. Map to domain (via aggregator or MapToDomain)
            var domains = pipeline.Aggregator is not null
                ? pipeline.Aggregator.Aggregate(rows, context)
                : pipeline.MapToDomain(rows, context);

            var domainList = domains as IList<TDomain> ?? domains.ToList();
            logger.LogInformation("Pipeline {Pipeline}: mapped {Count} domain entities", pipelineName,
                domainList.Count);

            foreach (var obs in observers) obs.OnAfterMapping(pipelineName, domainList.Count);

            // 10. Set domain in context (unless dry-run)
            if (!options.DryRun) context.SetDomain(domainList);

            var validRowCount = rows.Count;

            // 11. ProcessAsync (unless dry-run)
            if (!options.DryRun)
            {
                var domainReadOnly = domainList as IReadOnlyList<TDomain> ?? domainList.ToList().AsReadOnly();
                var result = await pipeline.ProcessAsync(domainReadOnly, context).ConfigureAwait(false);

                if (result.HasErrors)
                    allErrors.AddRange(result.Errors);
                if (result.Warnings.Count > 0)
                    allWarnings.AddRange(result.Warnings);

                logger.LogInformation("Pipeline {Pipeline}: COMPLETED - {DataCount} entities processed",
                    pipelineName, result.Data.Count);
            }
            else
            {
                logger.LogInformation(
                    "Pipeline {Pipeline}: DRY-RUN completed - {DataCount} entities would be processed",
                    pipelineName, domainList.Count);
            }

            RecordReport(context, pipelineName, sheet, range, rows.Count, validRowCount, allErrors, allWarnings, sw,
                true);
            foreach (var obs in observers) obs.OnCompleted(pipelineName, sw.Elapsed, allErrors.Count == 0);
        }
        catch (GSheetImportException)
        {
            foreach (var obs in observers) obs.OnCompleted(pipelineName, sw.Elapsed, false);
            throw;
        }
        catch (Exception ex)
        {
            foreach (var obs in observers) obs.OnCompleted(pipelineName, sw.Elapsed, false);
            logger.LogError(ex, "Pipeline {Pipeline}: error occurred during import", pipelineName);
            if (options.FailOnImportErrors)
            {
                if (ex is GSheetPreloadException or GSheetImportBatchException)
                    throw;

                throw GSheetImportException.ProcessingFailure(pipelineName, sheet, range, ex);
            }
        }
    }

    private static void RecordReport(
        GSheetImportContext context,
        string importerName,
        string sheetName,
        string range,
        int totalRows,
        int validRows,
        IReadOnlyList<GSheetValidationError> errors,
        IReadOnlyList<GSheetWarning> warnings,
        Stopwatch sw,
        bool isPipeline)
    {
        sw.Stop();
        context.Report.Add(new GSheetImportReport.ImporterReport
        {
            ImporterName = importerName,
            SheetName = sheetName,
            Range = range,
            TotalRows = totalRows,
            ValidRows = validRows,
            Errors = errors,
            Warnings = warnings,
            Duration = sw.Elapsed,
            UsedPipelineApi = isPipeline
        });
    }

    private static GSheetPreloadException CreatePreloadException(
        IEnumerable<(string SheetName, string Range)> sheetRanges,
        Exception ex)
    {
        return new GSheetPreloadException(
            sheetRanges.Select(static item => new GSheetImportTarget(item.SheetName, item.Range)).ToArray(),
            ex);
    }

    private static List<ImporterNode> BuildNodes(IEnumerable<Type> types)
    {
        var nodes = new List<ImporterNode>();

        foreach (var t in types)
        {
            var attr = t.GetCustomAttribute<GSheetImporterAttribute>();
            if (attr is not null)
            {
                nodes.Add(new ImporterNode(t, attr.SheetName, attr.Range, attr.Needs));
                continue;
            }

            // Check if this is a registered entity adapter
            var entityReg = GSheetImporterRegistry.GetStaticEntityRegistrations()
                .FirstOrDefault(r => r.AdapterType == t);

            if (entityReg is null)
                throw new InvalidOperationException(
                    $"Importer '{t.Name}' is missing [GSheetImporter] attribute and has no entity registration.");

            nodes.Add(new ImporterNode(t, entityReg.SheetName, entityReg.Range, entityReg.Needs));
        }

        return nodes;
    }

    private static List<List<ImporterNode>> TopologicallySort(List<ImporterNode> importers)
    {
        var depths = new Dictionary<Type, int>();
        var visiting = new HashSet<Type>();

        foreach (var node in importers) ComputeDepth(node);

        var maxDepth = depths.Values.DefaultIfEmpty(0).Max();
        var levels = new List<List<ImporterNode>>(maxDepth + 1);
        for (var i = 0; i <= maxDepth; i++)
            levels.Add([]);

        foreach (var node in importers)
            levels[depths[node.Type]].Add(node);

        return levels;

        int ComputeDepth(ImporterNode node)
        {
            if (depths.TryGetValue(node.Type, out var cached))
                return cached;

            if (!visiting.Add(node.Type))
                throw new InvalidOperationException($"Cyclic dependency detected at {node.Type.Name}");

            var maxDepDepth = node.Needs
                .Select(dep =>
                    importers.FirstOrDefault(i => i.Type == dep) ??
                    throw new InvalidOperationException($"Dependency '{dep.Name}' for '{node.Type.Name}' not found."))
                .Select(ComputeDepth).Prepend(-1).Max();

            var depth = maxDepDepth + 1;
            visiting.Remove(node.Type);
            depths[node.Type] = depth;
            return depth;
        }
    }

    private static (Type row, Type domain) GetImporterGenericArgs(Type importerType)
    {
        var iface = importerType.GetInterfaces()
                        .FirstOrDefault(it =>
                            it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IGSheetPipeline<,>))
                    ?? importerType.GetInterfaces()
                        .First(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IGSheetImporter<,>));
        var args = iface.GetGenericArguments();
        return (args[0], args[1]);
    }

    private record ImporterNode(Type Type, string SheetName, string Range, Type[] Needs);
}
