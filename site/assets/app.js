// No analytics, cookies, tracking, or external requests are performed by this site.
const OBS_SETTINGS = Object.freeze({server: 'rtmp://127.0.0.1:1935/live', key: 'studio'});
for (const button of document.querySelectorAll('[data-copy-obs]')) {
  button.addEventListener('click', async () => {
    const kind = button.getAttribute('data-copy-obs');
    const value = kind === 'key' ? OBS_SETTINGS.key : OBS_SETTINGS.server;
    try {await navigator.clipboard.writeText(value); button.textContent = 'Copied';}
    catch {button.textContent = value;}
  });
}
// Optional, read-only progressive enhancement. Browsers without WebMCP ignore it.
if (document.modelContext?.registerTool) {
  const register = tool => Promise.resolve(document.modelContext.registerTool(tool)).catch(() => {});
  register({
    name:'get_relaydesk_obs_setup',
    description:'Read the publicly documented localhost OBS custom RTMP setup for RelayDesk. Does not access credentials or start streams.',
    inputSchema:{type:'object',properties:{},additionalProperties:false},
    annotations:{readOnlyHint:true},
    execute:async()=>({...OBS_SETTINGS,note:'RelayDesk must run locally. These are localhost ingest settings, not private platform stream credentials.'})
  });
  register({
    name:'get_relaydesk_platform_presets',
    description:'Read the documented initial streaming preset options for six named services. Does not validate account eligibility or stream status.',
    inputSchema:{type:'object',properties:{},additionalProperties:false},
    annotations:{readOnlyHint:true},
    execute:async()=>({
      YouTube:'3840x2160 30fps (adjustable to 60fps)',
      Rumble:'1920x1080 30fps initially (4K profile configurable, platform acceptance unverified)',
      Facebook:'1920x1080 30fps', LinkedIn:'1920x1080 30fps',
      TikTok:'720x1280 30fps portrait', Instagram:'720x1280 30fps portrait',
      warning:'Source-code prototype, no verified production streaming or account eligibility.'
    })
  });
}
