const fs = require('fs');

function readStdin() {
  try {
    return fs.readFileSync(0, 'utf-8');
  } catch (e) {
    return '';
  }
}

function evaluate() {
  const inputRaw = readStdin();
  if (!inputRaw || inputRaw.trim().length === 0) {
    console.log(JSON.stringify({ decision: 'allow' }));
    return;
  }

  let hookData;
  try {
    hookData = JSON.parse(inputRaw);
  } catch (err) {
    console.log(JSON.stringify({ decision: 'allow' }));
    return;
  }

  const toolName = hookData.toolName || hookData.tool || '';
  const params = hookData.parameters || hookData.args || {};
  const targetFile = (params.TargetFile || params.targetFile || params.filePath || params.path || '').replace(/\\/g, '/');
  const content = (params.CodeContent || params.ReplacementContent || params.content || params.code || '') + ' ' + targetFile;

  const isSrc = targetFile.includes('/src/') || targetFile.startsWith('src/');
  const isSpecs = targetFile.includes('/specs/') || targetFile.startsWith('specs/');

  // 1. Cross-platform enforcement: No Win32/WPF in src/
  if (isSrc) {
    const forbiddenWin = ['System.Windows', 'PresentationFramework', 'Microsoft.Win32', 'DllImport'];
    for (const token of forbiddenWin) {
      const regex = new RegExp(`\\b${token}\\b`, 'i');
      if (regex.test(content)) {
        console.log(JSON.stringify({
          decision: 'deny',
          reason: `CleanRoomGuard: Forbidden Windows-specific dependency '${token}' detected in cross-platform target: ${targetFile}`
        }));
        process.exit(0);
      }
    }
  }

  // 2. Strict Clean-Room Chinese Wall enforcement: No proprietary brands in src/ or specs/
  if (isSrc || isSpecs) {
    const forbiddenBrands = ['Gemvision', 'MatrixGold', 'Stuller'];
    for (const brand of forbiddenBrands) {
      const regex = new RegExp(`\\b${brand}\\b`, 'i');
      if (regex.test(content)) {
        console.log(JSON.stringify({
          decision: 'deny',
          reason: `CleanRoomGuard: Proprietary signature '${brand}' strictly denied under Chinese Wall protocol in target: ${targetFile}`
        }));
        process.exit(0);
      }
    }
  }

  console.log(JSON.stringify({ decision: 'allow' }));
}

evaluate();
