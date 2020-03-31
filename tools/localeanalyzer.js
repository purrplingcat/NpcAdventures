const assetsDir = __dirname + "/../NpcAdventure/assets"
const unofficialLocaleDir = __dirname + "/../unofficial/locale";
const suffix = "json";
const supportedLocales = [
  {code: "pt-BR", name: "Portuguese", dir: assetsDir, official: true},
  {code: "fr-FR", name: "French", dir: assetsDir, official: true},
  {code: "zh-CN", name: "Chinese", dir: assetsDir, official: true},
  {code: "ja-JP", name: "Japanese", dir: unofficialLocaleDir + "/ja-JP", official: false},
  {code: "ru-RU", name: "Russian", dir: unofficialLocaleDir + "/ru-RU", official: false},
];
const knownContents = [
  "Data/Events",
  "Data/Quests",
  "Dialogue/Abigail",
  "Dialogue/Alex",
  "Dialogue/Elliott",
  "Dialogue/Emily",
  "Dialogue/Haley",
  "Dialogue/Harvey",
  "Dialogue/Leah",
  "Dialogue/Maru",
  "Dialogue/Penny",
  "Dialogue/Sam",
  "Dialogue/Sebastian",
  "Dialogue/Shane",
  "Strings/Buffs",
  "Strings/Mail",
  "Strings/SpeechBubbles",
  "Strings/Strings",
];

function readOriginal(asset) {
  const fs = require("fs");
  const path = require("path");
  const file = fs.readFileSync(path.resolve(assetsDir, asset + ".json"));
  const json = JSON.parse(file.toString().trim());

  return json;
}

function seekForMissing(locale, asset) {
  const missing = [];
  const fs = require("fs");
  const path = require("path");
  const json = readOriginal(asset);
  const localizedFilePath = path.resolve(locale.dir, asset + `.${locale.code}.json`);

  if (!fs.existsSync(localizedFilePath)) {
    console.log(`W01 (untranslated-asset): Asset '${asset}' is not translated into '${locale.code}'`);
    return missing.concat(Object.keys(json) || []);
  }

  const localizedFile = fs.readFileSync(localizedFilePath);
  const localizedJson = JSON.parse(localizedFile.toString().trim());

  for (let key of Object.keys(json)) {
    if (!Object.keys(localizedJson).includes(key)) {
      console.log(`W02 (untranslated-key): Missing '${locale.code}' localization for key '${key}' in asset '${asset}'`);
      missing.push(key);
    }
  }

  return missing;
}

function coverage(asset, misingKeysCount) {
  const keyCount = Object.keys(readOriginal(asset)).length;

  return {
    total: keyCount,
    covered: keyCount - misingKeysCount,
  };
}

function walk(locale, contents) {
  const report = [];
  for (let asset of contents) {
    try {
      const missing = seekForMissing(locale, asset);
      report.push({asset, missing, coverage: coverage(asset, missing.length)})
    } catch (error) {
      const json = readOriginal(asset);
      const missing = Object.keys(json) || [];

      report.push({asset, missing: [], error: error.message, coverage: coverage(asset, missing.length)});
      console.log(`E01 (general-error): An error occured while analysing locale '${locale.code}' asset '${asset}'`);
    }
  }

  return report;
}

function generateStats(fullReport) {
  const stats = [];

  for (let report of fullReport.reports) {
    //console.log(report.analysis);

    const total = report.analysis.reduce((ac, cur) => ac + cur.coverage.total || 0, 0);
    const covered = report.analysis.reduce((ac, cur) => ac + cur.coverage.covered || 0, 0)

    stats.push({
      locale: report.locale,
      label: report.label,
      official: report.official,
      failed: !!report.analysis.find(cur => cur.error),
      coverage: {
        total,
        covered,
        percentage: covered / total
      }
    });
  }

  return stats;
}

function analyze() {
  const report = {
    date: new Date(),
    title: "NPC Adventures localization coverage report",
    reports: [],
    stats: null,
  };

  console.log("Analyzing assets...");

  for (let locale of supportedLocales) {
    report.reports.push({
      locale: locale.code, 
      label: locale.name, 
      official: locale.official, 
      analysis: walk(locale, knownContents)
    });
  }

  console.log("Generating stats...");
  report.stats = generateStats(report);

  return report;
}

function mark(stat) {
  if (stat.failed) {
    return "F";
  }

  if (stat.coverage.percentage <= 0.5) {
    return "!"
  }

  if (stat.coverage.percentage < 0.8) {
    return "*"
  }

  return " ";
}

const analysis = analyze();

for (let stat of analysis.stats) {
  console.log(` ${mark(stat)} Locale: ${stat.label} (${stat.locale}) - Covered ${stat.coverage.covered} entries of ${stat.coverage.total} (${Number(stat.coverage.percentage * 100).toFixed(2)}%)`);
}

const reportFile = process.argv[2] || "report.json";
require("fs").writeFileSync(reportFile, JSON.stringify(analysis));
console.log(`Report written to ${reportFile}`);
