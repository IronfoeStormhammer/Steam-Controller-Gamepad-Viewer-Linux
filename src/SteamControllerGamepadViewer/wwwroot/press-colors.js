// Shared by the OBS overlay and the settings page's preview. No controller state is duplicated.
(() => {
  let config = {mode: "global", globalColor: "#25a7ff", controls: {}};
  let fingerprint = "";
  const defaults = new Map();
  const filterCache = new Map();
  const painted = new WeakMap();
  const valid = value => /^#[0-9a-f]{6}$/i.test(value);
  function colorFor(name) {
    if (config.configured === false && (params.has("btnPressed") || params.has("buttonPressed"))) return "#" + viewStyle.buttonPressed.hex;
    return config.mode === "per-control" && valid(config.controls?.[name]) ? config.controls[name] : config.globalColor;
  }
  function paint(element, name) {
    if (!element) return;
    const color = colorFor(name);
    if (!valid(color)) return;
    element.classList.toggle("press-colors-custom", color.toLowerCase() !== "#25a7ff");
    const signature = color + "|" + element.getAttribute("class") + "|" + name;
    if (painted.get(element) === signature) return;
    painted.set(element, signature);
    const rgb = [1,3,5].map(i => parseInt(color.slice(i, i+2),16));
    element.style.setProperty("--button-pressed", color);
    element.style.setProperty("--button-pressed-rgb", rgb.join(" "));
    element.style.setProperty("--active", color);
    element.style.setProperty("--active-strong", color);
    if (element.hasAttribute("data-control") && !element.classList.contains("active") && !document.body.classList.contains("preview-all")) {
      element.style.removeProperty("filter");
      element.style.removeProperty("fill");
      return;
    }
    // SVG paint servers do not inherit the referring element's CSS variables.
    // Give each control its own colorized copy while retaining masks/glow strengths.
    const style = getComputedStyle(element);
    const original = defaults.get(element) || {};
    for (const property of ["filter", "fill"]) {
      if (!original[property] && style[property].includes("url(") && !style[property].includes("#press-color-")) original[property] = style[property];
    }
    defaults.set(element, original);
    for (const [property, value] of Object.entries(original)) {
      const match = value.match(/url\(["']?(?:[^#]*#)([^)"']+)["']?\)/);
      if (!match) continue;
      const source = document.getElementById(match[1]);
      if (!source) continue;
      const key = match[1] + "|" + name;
      let clone = filterCache.get(key);
      if (!clone) {
        clone = source.cloneNode(true);
        clone.id = "press-color-" + filterCache.size;
        source.parentNode.appendChild(clone);
        filterCache.set(key, clone);
      }
      for (const stop of clone.querySelectorAll("stop")) {
        const sourceStop = source.querySelectorAll("stop")[[...clone.querySelectorAll("stop")].indexOf(stop)];
        if (sourceStop?.getAttribute("stop-color") === "var(--button-pressed)") stop.setAttribute("stop-color", color);
      }
      for (const flood of clone.querySelectorAll(".pressed-glow-color")) flood.style.setProperty("flood-color", color);
      for (const matrix of clone.querySelectorAll("feColorMatrix")) {
        matrix.setAttribute("values", color.toLowerCase() === "#25a7ff"
          ? source.querySelector("feColorMatrix").getAttribute("values")
          : `0 0 0 0 ${rgb[0]/255} 0 0 0 0 ${rgb[1]/255} 0 0 0 0 ${rgb[2]/255} 0 0 0 0.72 0`);
      }
      element.style.setProperty(property, `url(#${clone.id})`, property === "fill" ? "important" : "");
    }
  }
  function refresh() {
    for (const element of document.querySelectorAll("[data-control], [data-control-label]"))
      paint(element, element.dataset.control || element.dataset.controlLabel);
    for (const side of ["left", "right"]) {
      const stick = document.querySelector(`[data-stick-dot="${side}"]`);
      paint(stick, `${side}-stick-${stick?.classList.contains("pressed") ? "press" : "touch"}`);
      const pad = document.querySelector(`[data-pad="${side}"]`);
      paint(pad, `${side}-pad-${pad?.classList.contains("pressed") ? "click" : "touch"}`);
    }
  }
  window.PressColors = {refresh, apply(value) { config = value; refresh(); }};
  async function update() {
    try {
      const response = await fetch("/api/press-colors", {cache:"no-store"});
      if (!response.ok) return;
      const value = await response.json();
      const next = JSON.stringify(value);
      if (next !== fingerprint) { fingerprint = next; config = value; refresh(); }
    } catch { /* Keep the last valid colors during a server restart. */ }
  }
  // Capture paint servers in their active style without altering controller input.
  // They are also captured lazily when state activates a control.
  update();
  setInterval(update, 400);
})();
