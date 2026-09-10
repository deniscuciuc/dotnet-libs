/**
 * CoreLibs LiveConfig example spreadsheet bootstrapper.
 *
 * Paste this file into Extensions > Apps Script for a Google Sheet,
 * save it, then run prepareLiveConfigExampleSheets().
 *
 * The script creates or replaces all sheets required by the example:
 * - Legacy importer API: Bonuses, GameSettings
 * - Pipeline API: Currencies, Items, ItemPrices, ItemBundles, TournamentRewards, Tournaments
 * - Entity API: reuses the same pipeline sheets above
 */

function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('LiveConfig Example')
    .addItem('Prepare example sheets', 'prepareLiveConfigExampleSheets')
    .addToUi();
}

function prepareLiveConfigExampleSheets() {
  const ui = SpreadsheetApp.getUi();
  const answer = ui.alert(
    'Prepare LiveConfig example sheets?',
    'This will create or overwrite the example sheets used by the LiveConfig worker sample.',
    ui.ButtonSet.OK_CANCEL
  );

  if (answer !== ui.Button.OK) {
    return;
  }

  const spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
  const definitions = getSheetDefinitions_();

  definitions.forEach(function (definition, index) {
    upsertSheet_(spreadsheet, definition, index + 1);
  });

  spreadsheet.toast(
    'Prepared ' + definitions.length + ' sheets for the LiveConfig example.',
    'LiveConfig Example',
    5
  );
}

function getSheetDefinitions_() {
  return [
    {
      name: 'Bonuses',
      tabColor: '#2563eb',
      headers: ['Name', 'Amount', 'Currency', 'MinLevel', 'IsActive'],
      rows: [
        ['Welcome Bonus', 100, 'USD', 1, true],
        ['Daily Spin', 10, 'USD', 5, true],
        ['VIP Cashback', 500, 'USD', 20, false]
      ]
    },
    {
      name: 'GameSettings',
      tabColor: '#2563eb',
      headers: ['Key', 'Value'],
      rows: [
        ['MaxPlayers', '100'],
        ['RoundDurationSeconds', '60'],
        ['MinBetAmount', '1.00'],
        ['MaxBetAmount', '1000.00'],
        ['MaintenanceMode', 'false']
      ]
    },
    {
      name: 'Currencies',
      tabColor: '#16a34a',
      headers: ['Code', 'Name', 'DecimalPlaces'],
      rows: [
        ['USD', 'US Dollar', 2],
        ['EUR', 'Euro', 2],
        ['GEM', 'Premium Gem', 0]
      ]
    },
    {
      name: 'Items',
      tabColor: '#16a34a',
      headers: ['ItemId', 'Name', 'Rarity', 'Category'],
      rows: [
        ['itm_sword_01', 'Knight Sword', 'Rare', 'Weapon'],
        ['itm_shield_01', 'Aegis Shield', 'Epic', 'Armor'],
        ['itm_potion_01', 'Health Potion', 'Common', 'Consumable'],
        ['itm_key_01', 'Dungeon Key', 'Uncommon', 'Utility']
      ]
    },
    {
      name: 'ItemPrices',
      tabColor: '#16a34a',
      headers: ['ItemId', 'Currency', 'Price', 'DiscountPrice', 'IsActive'],
      rows: [
        ['itm_sword_01', 'USD', 24.99, 19.99, true],
        ['itm_shield_01', 'USD', 49.99, 39.99, true],
        ['itm_potion_01', 'GEM', 15, 10, true],
        ['itm_key_01', 'EUR', 5.99, '', true]
      ]
    },
    {
      name: 'ItemBundles',
      tabColor: '#16a34a',
      headers: ['BundleId', 'Name', 'ItemId', 'Currency', 'BundlePrice'],
      rows: [
        ['bundle_starter', 'Starter Pack', 'itm_sword_01', 'USD', 29.99],
        ['bundle_starter', 'Starter Pack', 'itm_potion_01', 'USD', 29.99],
        ['bundle_tank', 'Tank Pack', 'itm_shield_01', 'EUR', 44.99],
        ['bundle_tank', 'Tank Pack', 'itm_key_01', 'EUR', 44.99]
      ]
    },
    {
      name: 'TournamentRewards',
      tabColor: '#7c3aed',
      headers: ['TournamentId', 'PlaceFrom', 'PlaceTo', 'RewardCurrency', 'Amount', 'Multiplier'],
      rows: [
        ['tour_daily_01', 1, 1, 'GEM', 500, 2.0],
        ['tour_daily_01', 2, 5, 'GEM', 150, 1.25],
        ['tour_daily_01', 6, 20, 'USD', 10, 1.0],
        ['tour_weekly_01', 1, 1, 'USD', 250, 3.0],
        ['tour_weekly_01', 2, 10, 'USD', 75, 1.5]
      ]
    },
    {
      name: 'Tournaments',
      tabColor: '#7c3aed',
      headers: ['TournamentId', 'Name', 'Description', 'EntryCurrency', 'EntryFee', 'MinPlayers', 'MaxPlayers', 'Status'],
      rows: [
        ['tour_daily_01', 'Daily Arena', 'Fast daily PvP event', 'USD', 5, 2, 128, 'Active'],
        ['tour_weekly_01', 'Weekly Championship', 'Long-form competitive ladder', 'GEM', 25, 8, 512, 'Draft']
      ]
    }
  ];
}

function upsertSheet_(spreadsheet, definition, index) {
  var sheet = spreadsheet.getSheetByName(definition.name);

  if (!sheet) {
    sheet = spreadsheet.insertSheet(definition.name, index - 1);
  }

  sheet.clear();
  sheet.setFrozenRows(1);
  sheet.setTabColor(definition.tabColor);

  var headerRange = sheet.getRange(1, 1, 1, definition.headers.length);
  headerRange.setValues([definition.headers]);
  headerRange
    .setFontWeight('bold')
    .setBackground('#111827')
    .setFontColor('#ffffff');

  if (definition.rows.length > 0) {
    sheet
      .getRange(2, 1, definition.rows.length, definition.headers.length)
      .setValues(definition.rows);
  }

  sheet.autoResizeColumns(1, definition.headers.length);
  sheet.getRange(1, 1, Math.max(definition.rows.length + 1, 2), definition.headers.length)
    .setVerticalAlignment('middle');
}