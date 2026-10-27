# Publish RelayDesk as a public GitHub repository and website

## One-time action to create the repository

Your connected GitHub installation currently cannot create a new repository. On the GitHub website, create a **new public** repository named `RelayDesk-Windows` under your selected account. Choose **Add README = off, .gitignore = none, license = none** when uploading by git. Alternatively, create with a README and use the GitHub web file-upload method, but reconcile existing files rather than force-pushing.

Repository creation: https://github.com/new

## Upload safely from Windows

Install Git for Windows, extract this archive, and open PowerShell in the project folder:

```powershell
# Replace ACCOUNT with your actual GitHub account name.
git init -b main
git add .
git status
git commit -m "Initial public RelayDesk Windows source and website"
git remote add origin https://github.com/ACCOUNT/RelayDesk-Windows.git
git push -u origin main
```

Before committing, inspect `git status` and confirm that no stream keys, configuration secrets, FFmpeg command lines or private customer data are being published. The sample app uses placeholder stream URLs only.

## Enable website

1. Open the repository → **Settings → Pages**.
2. Under **Build and deployment**, choose **GitHub Actions**.
3. The included `.github/workflows/pages.yml` builds the static site, applies the correct canonical URL and generates `robots.txt`, `sitemap.xml`, `llms.txt`, and `llms-full.txt`.
4. Check the Actions tab for a successful **Deploy website** run.
5. The default project address should be `https://ACCOUNT.github.io/RelayDesk-Windows/` if using GitHub Pages with a regular account.
6. Optionally add a custom domain in Pages settings and update the deployment `SITE_URL` environment variable accordingly; configure DNS and HTTPS per GitHub docs.

The website is not live until the repository exists, Pages is enabled, and deployment succeeds. A live URL or Google indexing is never guaranteed by a workflow file alone.

## Build-check workflow

`.github/workflows/windows-build.yml` attempts a .NET 10 Windows release build and publishes an **unsigned** CI artifact. It does **not** bundle proprietary or third-party binaries, test real platform streams, or create a signed installer.

## Search discovery

When deployment is confirmed, submit the resulting sitemap URL to Google Search Console and Bing Webmaster Tools. GitHub Pages may be crawled without registration, but indexing and rich results are not guaranteed.

## Release checklist

- [ ] Full Windows build succeeds in GitHub Actions
- [ ] At least one private/unlisted end-to-end live streaming test
- [ ] Six destination test results are documented individually
- [ ] Dependencies and licenses reviewed before bundling binaries
- [ ] Installer is signed and malware-scanned before public download
- [ ] GitHub Pages URL confirmed and Search Console property verified
