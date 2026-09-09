const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const {inventory,verify}=require('./check-web-build.cjs');
function fixture(t) {
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'throwntogether-artifact-'));
  t.after(()=>fs.rmSync(root,{recursive:true,force:true}));
  fs.mkdirSync(path.join(root,'Build'));
  for(const name of ['index.html','.nojekyll','Build/a.wasm','Build/a.data','Build/a.loader.js','Build/a.framework.js']) fs.writeFileSync(path.join(root,name),'fixture');
  fs.writeFileSync(path.join(root,'build-info.json'),JSON.stringify({sourceCommit:'a'.repeat(40)})); return root;
}
test('manifest detects changed and missing build bytes',t=>{
  const root=fixture(t); fs.writeFileSync(path.join(root,'build-manifest.json'),JSON.stringify(inventory(root)));
  assert.equal(verify(root).files.length,7);
  fs.appendFileSync(path.join(root,'Build/a.wasm'),'changed'); assert.throws(()=>verify(root),/differs/);
  fs.unlinkSync(path.join(root,'Build/a.wasm')); assert.throws(()=>verify(root),/Missing .wasm/);
});
test('source files and compressed payloads cannot be published',t=>{
  const root=fixture(t); fs.writeFileSync(path.join(root,'Build/Secret.cs'),'source'); assert.throws(()=>inventory(root),/Source/);
  fs.unlinkSync(path.join(root,'Build/Secret.cs')); fs.writeFileSync(path.join(root,'Build/a.gz'),'compressed'); assert.throws(()=>inventory(root),/compressed/);
});
test('snapshot identity is required',t=>{
  const root=fixture(t); fs.writeFileSync(path.join(root,'build-info.json'),'{}'); assert.throws(()=>inventory(root),/identity/);
});
