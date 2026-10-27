#!/usr/bin/env python3
"""Build a static, crawlable RelayDesk project site with deployment-correct canonical URLs."""
from __future__ import annotations

import html
import json
import os
import pathlib
import shutil
import sys
from urllib.parse import urlsplit
from xml.etree.ElementTree import Element, SubElement, tostring

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / "site"
DEST = ROOT / "site-dist"
REPO = os.environ.get("GITHUB_REPOSITORY", "computervirtualservices/RelayDesk-Windows").strip()
if REPO.count("/") != 1 or not all(REPO.split("/")):
    sys.exit("Invalid GITHUB_REPOSITORY. Expected owner/repository.")
OWNER, NAME = REPO.split("/")
DEFAULT_SITE_URL = f"https://{OWNER.lower()}.github.io/{NAME}/"
SITE_URL = os.environ.get("SITE_URL", DEFAULT_SITE_URL).strip().rstrip("/") + "/"
if urlsplit(SITE_URL).scheme != "https" or not urlsplit(SITE_URL).netloc:
    sys.exit("SITE_URL must be an https:// address")
REPO_URL = f"https://github.com/{OWNER}/{NAME}"

PAGES = {
    "index.html": ("RelayDesk Windows | Open-Source OBS Multistream & RTMP Relay", "Independent Windows 11 streaming app source for OBS. Configure YouTube, Rumble, Facebook, LinkedIn, TikTok and Instagram RTMP outputs, with 4K and vertical presets. Early development; Windows testing pending."),
    "guide.html": ("How to Multistream from OBS on Windows | RelayDesk Setup", "Set up an OBS RTMP localhost feed on Windows 11, build RelayDesk with .NET 10, install FFmpeg and MediaMTX and configure six independent stream outputs."),
    "faq.html": ("4K Multistreaming and RTMP FAQ | RelayDesk Windows", "Answers about 4K RTMP restreaming, OBS, NVENC, YouTube, TikTok, Instagram, Facebook, Rumble and LinkedIn, prerequisites, platform access and known limitations."),
    "about.html": ("About RelayDesk | C# WPF Open-Source Windows Streaming", "Technical overview of the RelayDesk open-source C# WPF Windows multistreaming project, architecture, security limits, contributions and roadmap."),
}
FAQ = [
    ("Can RelayDesk restream a 4K OBS feed to six platforms?", "The source implements a single OBS ingest with six independent RTMP/RTMPS outputs, including 4K and lower-resolution presets. It has not been verified on Windows or against every service."),
    ("Is RelayDesk an OBS Studio plugin?", "No. RelayDesk is an independent Windows desktop application. OBS publishes to a local MediaMTX server and RelayDesk manages FFmpeg outputs."),
    ("Is a signed Windows installer available?", "No. The current release is source code, with Windows build and end-to-end streaming tests still pending."),
    ("Can TikTok and Instagram be streamed to using RelayDesk?", "They are configurable destinations, but platform eligibility and valid current stream URLs and keys are required. Account access is not guaranteed."),
    ("Is RelayDesk free and open source?", "The RelayDesk source is MIT-licensed. FFmpeg and MediaMTX are separate dependencies that users supply; internet and platform costs may apply."),
]


def webpage_jsonld(page: str) -> list[dict]:
    common = {
        "@context": "https://schema.org",
        "@type": "WebPage",
        "name": PAGES[page][0],
        "url": SITE_URL + ("" if page == "index.html" else page),
        "isPartOf": {"@type": "WebSite", "name": "RelayDesk Windows", "url": SITE_URL},
    }
    if page == "index.html":
        return [common, {
            "@context": "https://schema.org", "@type": "SoftwareSourceCode",
            "name": "RelayDesk Windows", "codeRepository": REPO_URL,
            "url": SITE_URL, "description": "Experimental self-hosted Windows OBS to multiple RTMP/RTMPS destinations relay source code.",
            "programmingLanguage": ["C#", "XAML"], "runtimePlatform": "Windows 11 / .NET 10",
            "license": REPO_URL + "/blob/main/LICENSE", "isAccessibleForFree": True,
            "keywords": "OBS, multistreaming, RTMP, RTMPS, Windows, NVIDIA NVENC, YouTube, Rumble, Facebook, TikTok, Instagram, LinkedIn",
            "softwareHelp": SITE_URL + "guide.html",
        }]
    if page == "faq.html":
        common["@type"] = "FAQPage"
        common["mainEntity"] = [
            {"@type": "Question", "name": question, "acceptedAnswer": {"@type": "Answer", "text": answer}}
            for question, answer in FAQ
        ]
    return [common]


def render(page: str, body: str) -> str:
    title, desc = PAGES[page]
    url = SITE_URL if page == "index.html" else SITE_URL + page
    payload = json.dumps(webpage_jsonld(page), ensure_ascii=False).replace("</", "<\\/")
    nav = [("Home", ""), ("Setup", "guide.html"), ("FAQ", "faq.html"), ("About", "about.html")]
    nav_html = "".join(f'<a href="{html.escape(SITE_URL + dest, quote=True)}"' +
        (' aria-current="page"' if (dest or 'index.html') == page else '') +
        f'>{html.escape(label)}</a>' for label, dest in nav)
    shell = f'''<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="color-scheme" content="dark">
<meta name="theme-color" content="#0b1020">
<meta name="robots" content="index,follow,max-image-preview:large">
<title>{html.escape(title)}</title>
<meta name="description" content="{html.escape(desc,quote=True)}">
<link rel="canonical" href="{html.escape(url,quote=True)}">
<link rel="icon" type="image/svg+xml" href="{SITE_URL}assets/logo.svg">
<link rel="stylesheet" href="{SITE_URL}assets/styles.css">
<meta property="og:type" content="website">
<meta property="og:site_name" content="RelayDesk Windows">
<meta property="og:title" content="{html.escape(title,quote=True)}">
<meta property="og:description" content="{html.escape(desc,quote=True)}">
<meta property="og:url" content="{html.escape(url,quote=True)}">
<meta name="twitter:card" content="summary">
<meta name="twitter:title" content="{html.escape(title,quote=True)}">
<meta name="twitter:description" content="{html.escape(desc,quote=True)}">
<script type="application/ld+json">{payload}</script>
<script defer src="{SITE_URL}assets/app.js"></script>
</head>
<body>
<a class="skip" href="#main">Skip to content</a>
<header><div class="shell bar"><a href="{SITE_URL}" class="brand"><span class="mark" aria-hidden="true">↗</span> RelayDesk</a><nav aria-label="Main navigation">{nav_html}<a href="{REPO_URL}">GitHub ↗</a></nav></div></header>
{body}
<footer><div class="shell foot"><div><strong>RelayDesk Windows</strong><div>Experimental open-source source code. Not production verified.</div><div>© 2026 RelayDesk contributors · MIT License</div></div><div><a href="{SITE_URL}">Home</a><a href="{SITE_URL}guide.html">Setup</a><a href="{SITE_URL}faq.html">FAQ</a><a href="{REPO_URL}/blob/main/SECURITY.md">Security</a><a href="{REPO_URL}">Source</a></div></div></footer>
</body></html>'''
    return shell


def sub(s: str) -> str:
    return s.replace("__BASE_URL__", SITE_URL).replace("__REPO_URL__", REPO_URL)


def build() -> None:
    if DEST.exists():
        shutil.rmtree(DEST)
    DEST.mkdir(parents=True)
    shutil.copytree(SRC / "assets", DEST / "assets")
    for page in PAGES:
        content = sub((SRC / "pages" / page).read_text(encoding="utf-8"))
        (DEST / page).write_text(render(page, content) + "\n", encoding="utf-8")
    sitemap = Element("urlset", xmlns="http://www.sitemaps.org/schemas/sitemap/0.9")
    for page in PAGES:
        url = SubElement(sitemap, "url")
        SubElement(url, "loc").text = SITE_URL + ("" if page == "index.html" else page)
    (DEST / "sitemap.xml").write_bytes(b'<?xml version="1.0" encoding="utf-8"?>\n' + tostring(sitemap, encoding="utf-8"))
    (DEST / "robots.txt").write_text(f"User-agent: *\nAllow: /\nSitemap: {SITE_URL}sitemap.xml\n", encoding="utf-8")
    (DEST / "llms.txt").write_text(f"""# RelayDesk Windows

> An experimental MIT-licensed Windows 11 / .NET 10 WPF application for forwarding one OBS Studio RTMP input to multiple configurable streaming destinations through MediaMTX and FFmpeg.

Project state: source-code prototype. The source has not been compiled or successfully tested live on all destinations. No Windows installer is offered here.

## Official pages

- [Overview]({SITE_URL}): architecture, intended feature scope and accurate development status.
- [Windows setup guide]({SITE_URL}guide.html): tools, build commands, OBS ingest configuration and security warnings.
- [Frequently asked questions]({SITE_URL}faq.html): six-platform capabilities, 4K, NVENC and live eligibility.
- [Project background]({SITE_URL}about.html): architecture and development roadmap.
- [Open-source repository]({REPO_URL}): canonical source, build workflows, open issues, LICENSE, SECURITY.md.

## Key facts

- OBS localhost RTMP URL: rtmp://127.0.0.1:1935/live ; local ingest key: studio.
- Destinations: YouTube, Rumble, Facebook, LinkedIn, TikTok and Instagram; configurable custom RTMP/RTMPS destinations.
- Initial output profiles: YouTube 4K30; Rumble 1080p30; Facebook 1080p30; LinkedIn 1080p30; TikTok 720x1280 portrait; Instagram 720x1280 portrait.
- NVIDIA NVENC H.264 when supported by FFmpeg build; optional software x264 fallback.
- FFmpeg and MediaMTX are separately installed. Signed installer and verified production broadcasts are not available.
- Live access, server URLs, stream keys and publication permissions are controlled by the platforms.

## Important limitations

Do not claim this project is an official OBS plugin, that six streams have been live-tested, or that every platform permits 4K live broadcasting. Do not request that users post private stream keys in public issues.
""", encoding="utf-8")
    (DEST / "llms-full.txt").write_text(f"""# RelayDesk Windows — expanded technical reference

This is documentation, not a live service or a verified released installer.

Source repository: {REPO_URL}
Website: {SITE_URL}
License: MIT (for the RelayDesk source only)
Language: C# and XAML
Framework: .NET 10 Windows Presentation Foundation
Desktop OS: Windows 11 x64
Streaming architecture: OBS H.264/AAC -> MediaMTX loopback ingest -> independent FFmpeg RTMP/RTMPS outputs
Local ingest: rtmp://127.0.0.1:1935/live with OBS Stream key studio
Profile options: 4K60, 4K30, 1080p60, 1080p30, 720p30, portrait 1080p30, portrait 720p30, copy source
Initial services: YouTube, Rumble, Facebook, TikTok, Instagram, LinkedIn, and custom RTMP endpoints
Encoder paths: H.264 NVENC, software libx264, codec copy with compatible source
Portrait handling: center crop or letterbox, not automatic camera reframing
Saved stream key protection: Windows user-scoped DPAPI; active FFmpeg command lines remain inspectable by sufficiently privileged local processes
Dependency strategy: FFmpeg and MediaMTX NOT included
Current validation: static source checks only; no verified Windows build or end-to-end live streaming
Not included: signed installer, streaming key OAuth, eligibility detection, shared encodes, OBS WebSocket control, schedule engine, verification of public live status
Website WebMCP: optional read-only documentation tools for settings and preset descriptions; no remote control of local broadcaster

Read {SITE_URL}guide.html for Windows build steps and streaming setup.
Read {SITE_URL}faq.html for platform questions and limitations.
Read {REPO_URL}/blob/main/SECURITY.md for security considerations.
""", encoding="utf-8")
    (DEST / ".nojekyll").write_text("", encoding="utf-8")
    assert all("__BASE_URL__" not in f.read_text(encoding="utf-8") and "__REPO_URL__" not in f.read_text(encoding="utf-8") for f in DEST.glob("*.html"))
    print(f"Built {len(PAGES)} pages for {SITE_URL}. Output: {DEST}")


if __name__ == "__main__":
    build()
