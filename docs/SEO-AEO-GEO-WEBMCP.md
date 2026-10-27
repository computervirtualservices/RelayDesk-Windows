# Search and AI-discovery launch plan

## Public positioning

Product: RelayDesk Windows. Target intent: Windows OBS multistreaming, open-source Restream alternatives, RTMP multiple outputs, 4K live restream, NVIDIA NVENC relay, TikTok/Instagram portrait OBS streaming.

Always describe the code as a **prototype** until a working Windows build and end-to-end destination tests prove otherwise.

## SEO (search engines)

- Hand-authored titles, descriptions, canonical URLs, Open Graph metadata and readable semantic HTML on every public page.
- Four distinct, factually grounded pages: overview, Windows setup, FAQ, project background.
- `SoftwareSourceCode`, `WebPage`, `WebSite`, and FAQ content marked up with schema.org JSON-LD. FAQ rich-result appearance is not guaranteed and Google limits eligibility for that result type.
- GitHub Pages Action computes canonical URLs and sitemap entries from the **actual** repository owner/name at deployment, avoiding accidental canonicals pointing at a guessed personal domain.
- `sitemap.xml` and `robots.txt` are generated. Note: on a GitHub **project** Pages site under `/RepoName/`, its `robots.txt` resides at that path, not at the host root, so it is not an authoritative root robots directive. Submit the sitemap in Search Console.
- Optional custom domain: configure in Pages settings plus GitHub's HTTPS and DNS guidance; update `SITE_URL` in the Pages workflow environment to ensure canonical, sitemap and metadata reflect it. Do not set two competing canonical origins.
- Prioritize real working release assets, performance data, and verified tutorials before seeking backlinks or promises of rankings.

## AEO (answer-engine optimization)

- Direct question/answer format on FAQ page and step-by-step OBS setup page.
- Answer each platform-specific question with constraints and access requirements. Avoid implying every destination supports 4K just because the video source is 4K.
- Cite upstream dependencies and platform guides in the README; do not fabricate results, testimonials, download counts or awards.

## GEO (generative-engine optimization)

- Publish factual project constraints, well-defined capabilities, architecture and current limitations in machine-readable `llms.txt` and `llms-full.txt`.
- Put primary facts in indexable HTML, rather than relying on `llms.txt` being respected by any particular AI system.
- Keep repository README, Pages content and structured data aligned. Update all three when release validation status changes.
- `llms.txt` has no universal adoption or ranking guarantee; here it is an optional supporting resource.

## WebMCP (optional web agent access)

- Uses the **proposed** `document.modelContext.registerTool` browser interface if available.
- Provides two read-only helper tools: `get_relaydesk_obs_setup` and `get_relaydesk_platform_presets`.
- Requires compatible WebMCP experimental browser support; intentionally does **not** control local OBS, collect credentials, start streams, open external accounts or change user settings.
- WebMCP is a W3C Community Group draft as of October 2026, not a finished W3C Recommendation. Do not advertise universal support.

## Release and webmaster checklist

1. Publish repository with MIT license and no private keys, tokens, logs or external binaries.
2. Set GitHub description, topics and homepage with `Publish-GitHub.ps1` or repo settings.
3. Enable GitHub Pages source **GitHub Actions** and review the deployment run.
4. Verify exact live canonical domain, 200 responses, sitemap entries and human-readable page content.
5. Verify the Pages origin in Google Search Console and Bing Webmaster Tools, then submit sitemap.
6. Check indexing and page performance; refine pages with verified search queries and genuine how-to examples.
7. When a real signed installer exists, add checksums, release notes, versioning and download/installation instructions.

Useful sources:
- https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site
- https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap
- https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data
- https://developer.chrome.com/docs/ai/webmcp
- https://webmachinelearning.github.io/webmcp/
