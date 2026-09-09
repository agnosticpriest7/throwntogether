// Static artifact verification only; generated files remain under ignored Builds/.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
function inventory(root) {
  const files=[];
  function walk(dir) {
    for(const item of fs.readdirSync(dir,{withFileTypes:true})) {
      const full=path.join(dir,item.name), relative=path.relative(root,full).replaceAll('\\','/');
      if(item.isSymbolicLink()) throw new Error(`Linked artifact: ${relative}`);
      if(item.isDirectory()) { walk(full); continue; }
      if(relative==='build-manifest.json') continue;
      if(!/^(Build|TemplateData|StreamingAssets)\//.test(relative) && !['index.html','web-shell.js','build-info.json','.nojekyll'].includes(relative)) throw new Error(`Unexpected artifact: ${relative}`);
      if(/\.(cs|unity|prefab|meta|gz|br|unityweb)$/i.test(relative)) throw new Error(`Source or compressed artifact: ${relative}`);
      const data=fs.readFileSync(full);
      if(data.length>=100*1024*1024) throw new Error(`Artifact exceeds GitHub file limit: ${relative}`);
      files.push({path:relative,bytes:data.length,sha256:crypto.createHash('sha256').update(data).digest('hex')});
    }
  }
  walk(root); files.sort((a,b)=>a.path.localeCompare(b.path));
  const names=files.map(f=>f.path);
  for(const name of ['index.html','build-info.json','.nojekyll']) if(!names.includes(name)) throw new Error(`Missing ${name}`);
  for(const extension of ['.wasm','.data','.loader.js','.framework.js']) if(!names.some(n=>n.startsWith('Build/')&&n.endsWith(extension))) throw new Error(`Missing ${extension}`);
  const info=JSON.parse(fs.readFileSync(path.join(root,'build-info.json'),'utf8').replace(/^\uFEFF/,''));
  if(!/^[0-9a-f]{40}$/.test(info.sourceCommit)) throw new Error('Invalid source identity');
  return {schemaVersion:1,sourceCommit:info.sourceCommit,totalBytes:files.reduce((n,f)=>n+f.bytes,0),files};
}
function verify(root) {
  const actual=inventory(root), expected=JSON.parse(fs.readFileSync(path.join(root,'build-manifest.json'),'utf8'));
  if(JSON.stringify(actual)!==JSON.stringify(expected)) throw new Error('Web artifact differs from its build manifest');
  return actual;
}
module.exports={inventory,verify};
if(require.main===module) {
  const [mode,root]=process.argv.slice(2);
  if(!root || !['--write','--check'].includes(mode)) throw new Error('Usage: node scripts/check-web-build.cjs --write|--check Builds/Web');
  if(mode==='--write') fs.writeFileSync(path.join(root,'build-manifest.json'),JSON.stringify(inventory(root),null,2)+'\n');
  const result=verify(root); console.log(`Verified ${result.files.length} Web files, ${result.totalBytes} bytes, source ${result.sourceCommit}`);
}
