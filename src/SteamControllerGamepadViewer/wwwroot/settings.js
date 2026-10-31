const mode = document.getElementById("mode");
const global = document.getElementById("global");
const controls = document.getElementById("controls");
const status = document.getElementById("status");
const frame = document.getElementById("preview");
const labels = {a:"A",b:"B",x:"X",y:"Y",lb:"LB",rb:"RB",lt:"L2",rt:"R2",lg2:"L4",lg1:"L5",rg2:"R4",rg1:"R5","touch-click":"Quick Access"};
let current = {mode:"global",globalColor:"#25a7ff",controls:{}};
let dirty = false;
function values() {
  const colors = {...current.controls};
  for (const input of controls.querySelectorAll("input")) {
    if (input.dataset.changed === "true" || Object.hasOwn(colors,input.name)) colors[input.name] = input.value;
  }
  return {mode:mode.value,globalColor:global.value,controls:colors};
}
function preview() { controls.hidden = mode.value !== "per-control"; frame.contentWindow?.PressColors?.apply(values()); }
function show(value) {
  current = value; dirty = false; mode.value = value.mode; global.value = value.globalColor;
  for (const input of controls.querySelectorAll("input")) {input.value = value.controls[input.name] || value.globalColor; delete input.dataset.changed;}
  preview();
}
async function request(path, options) {
  const response = await fetch(path,options);
  if (!response.ok) throw new Error("Could not save settings. Please try again.");
  return response.json();
}
document.getElementById("settings").addEventListener("input", event => {
  dirty = true;
  if(event.target.name) event.target.dataset.changed = "true";
  preview(); status.textContent = "Unsaved changes — preview only.";
});
document.getElementById("settings").addEventListener("submit", async event => {
  event.preventDefault();
  try {show(await request("/api/press-colors",{method:"PUT",headers:{"Content-Type":"application/json"},body:JSON.stringify(values())}));status.textContent="Saved. All overlays update automatically.";}
  catch(error) {status.textContent=error.message;}
});
document.getElementById("reset").addEventListener("click",async()=>{
  try {show(await request("/api/press-colors/reset",{method:"POST"}));status.textContent="Defaults restored: global blue (#25a7ff).";}
  catch(error) {status.textContent=error.message;}
});
frame.addEventListener("load",()=>preview());
// The iframe also polls saved settings; reapply unsaved edits to this preview only.
setInterval(()=>{if(dirty)preview()},500);
(async()=>{
 try {
  const names=await request("/api/press-colors/controls");
  for(const name of names){
   const label=document.createElement("label");label.textContent=labels[name] || name.replaceAll("-"," ").replace(/\b\w/g,c=>c.toUpperCase());
   const input=document.createElement("input");input.type="color";input.name=name;input.id="color-"+name;label.htmlFor=input.id;label.append(input);controls.append(label);
  }
  show(await request("/api/press-colors"));status.textContent="Ready. Default blue is #25a7ff.";
 } catch {status.textContent="Unable to load settings. Check that the viewer is running.";}
})();
