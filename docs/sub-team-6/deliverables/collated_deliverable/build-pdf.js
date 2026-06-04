// Renders collated-deliverable.md -> collated-deliverable.pdf
// Mermaid blocks are rendered client-side; everything else via marked + highlight.js.
// Uses the system-installed Chrome (no Chromium download).
const fs = require('fs');
const path = require('path');
const { marked } = require('marked');
const hljs = require('highlight.js');
const puppeteer = require('puppeteer-core');

const DIR = __dirname;
const MD = path.join(DIR, 'collated-deliverable.md');
const HTML = path.join(DIR, 'collated-deliverable.html');
const PDF = path.join(DIR, 'collated-deliverable.pdf');

const CHROME_CANDIDATES = [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
];
const chrome = CHROME_CANDIDATES.find(p => fs.existsSync(p));
if (!chrome) { console.error('No Chrome/Edge found'); process.exit(1); }

const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

const renderer = new marked.Renderer();
renderer.code = function (code, infostring) {
  const lang = (infostring || '').trim().split(/\s+/)[0];
  if (lang === 'mermaid') {
    return `<pre class="mermaid">${esc(code)}</pre>`;
  }
  let highlighted;
  try {
    highlighted = lang && hljs.getLanguage(lang)
      ? hljs.highlight(code, { language: lang }).value
      : hljs.highlightAuto(code).value;
  } catch (e) {
    highlighted = esc(code);
  }
  return `<pre class="code"><code class="hljs language-${lang || ''}">${highlighted}</code></pre>`;
};

marked.setOptions({ renderer, gfm: true, breaks: false, headerIds: true, mangle: false });

const mdSource = fs.readFileSync(MD, 'utf8');
const body = marked.parse(mdSource);
const mermaidLib = fs.readFileSync(path.join(DIR, 'mermaid.min.js'), 'utf8');
const hljsCss = fs.readFileSync(path.join(DIR, 'node_modules/highlight.js/styles/github.css'), 'utf8');

const html = `<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<style>
${hljsCss}
@page { size: A4; margin: 18mm 16mm; }
* { box-sizing: border-box; }
body { font-family: "Segoe UI", Arial, sans-serif; font-size: 10.5pt; line-height: 1.5; color: #1a1a1a; max-width: 100%; }
h1 { font-size: 20pt; border-bottom: 2px solid #444; padding-bottom: 4px; margin-top: 0.6em; }
h2 { font-size: 15pt; border-bottom: 1px solid #ccc; padding-bottom: 3px; margin-top: 1.2em; }
h3 { font-size: 12.5pt; margin-top: 1em; }
h4 { font-size: 11pt; margin-top: 0.9em; }
h1, h2, h3, h4 { page-break-after: avoid; }
p, li { orphans: 3; widows: 3; }
table { border-collapse: collapse; width: 100%; margin: 0.8em 0; font-size: 9.5pt; page-break-inside: avoid; }
th, td { border: 1px solid #bbb; padding: 4px 7px; text-align: left; vertical-align: top; }
th { background: #f0f0f0; }
code { font-family: "Cascadia Code", Consolas, monospace; font-size: 9pt; background: #f3f3f3; padding: 1px 3px; border-radius: 3px; }
pre.code { background: #f7f7f7; border: 1px solid #ddd; border-radius: 4px; padding: 10px 12px; overflow: visible;
  white-space: pre-wrap; word-wrap: break-word; font-size: 8.5pt; line-height: 1.4; page-break-inside: auto; }
pre.code code { background: none; padding: 0; font-size: inherit; white-space: pre-wrap; }
pre.mermaid { background: #fff; text-align: center; page-break-inside: avoid; margin: 1em 0; }
/* Cap BOTH width and height to the printable page box so tall diagrams (e.g. the
   CanvassDesktop before-sequence) scale down whole instead of overflowing and clipping.
   230mm leaves room for a heading line above the diagram on the same page. */
pre.mermaid svg { width: auto !important; height: auto !important;
  max-width: 100% !important; max-height: 230mm !important; }
img { max-width: 100%; height: auto; page-break-inside: avoid; }
blockquote { border-left: 4px solid #ccc; margin: 0.8em 0; padding: 0.2em 0 0.2em 1em; color: #555; }
a { color: #0b5cad; text-decoration: none; }
hr { border: none; border-top: 1px solid #ddd; margin: 1.2em 0; }
</style></head>
<body>
${body}
<script>${mermaidLib}</script>
<script>
  mermaid.initialize({ startOnLoad: false, theme: 'neutral', securityLevel: 'loose',
    flowchart: { useMaxWidth: true }, sequence: { useMaxWidth: true } });
  (async () => {
    const nodes = Array.from(document.querySelectorAll('pre.mermaid'));
    let ok = 0, fail = 0;
    for (let i = 0; i < nodes.length; i++) {
      const el = nodes[i];
      const src = el.textContent;
      try {
        const { svg } = await mermaid.render('mmd-' + i, src);
        el.innerHTML = svg;
        el.removeAttribute('class'); // keep styling container but avoid re-run
        el.classList.add('mermaid');
        ok++;
      } catch (e) {
        fail++;
        console.error('MERMAID-FAIL #' + i + ': ' + (e && e.message ? e.message : e));
        el.innerHTML = '<div style="color:#b00;font-size:8pt;">[diagram ' + i + ' failed to render]</div><pre style="font-size:7pt;">' +
          src.replace(/[&<>]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;'}[c])) + '</pre>';
      }
    }
    console.log('MERMAID-SUMMARY ok=' + ok + ' fail=' + fail);
    window.__renderDone = true;
  })();
</script>
</body></html>`;

fs.writeFileSync(HTML, html, 'utf8');
console.log('HTML written:', HTML, '(' + (html.length / 1024).toFixed(0) + ' KB)');

(async () => {
  const browser = await puppeteer.launch({
    executablePath: chrome,
    headless: 'new',
    args: ['--no-sandbox', '--allow-file-access-from-files'],
  });
  const page = await browser.newPage();
  page.on('console', m => { const t = m.text(); if (m.type() === 'error' || /MERMAID/.test(t)) console.log('PAGE:', t); });
  await page.goto('file://' + HTML.replace(/\\/g, '/'), { waitUntil: 'networkidle0', timeout: 120000 });
  await page.waitForFunction('window.__renderDone === true', { timeout: 120000 });
  await new Promise(r => setTimeout(r, 1500)); // settle layout
  await page.pdf({
    path: PDF, format: 'A4', printBackground: true,
    margin: { top: '18mm', bottom: '18mm', left: '16mm', right: '16mm' },
    displayHeaderFooter: true,
    headerTemplate: '<span></span>',
    footerTemplate: '<div style="font-size:8pt;color:#888;width:100%;text-align:center;">' +
      'iDaVIE Refactoring Proposal — Desktop GUI &amp; Client Shell · Team Alpha (Die Boks) · ' +
      'Page <span class="pageNumber"></span> / <span class="totalPages"></span></div>',
  });
  await browser.close();
  const kb = (fs.statSync(PDF).size / 1024).toFixed(0);
  console.log('PDF written:', PDF, '(' + kb + ' KB)');
})();
