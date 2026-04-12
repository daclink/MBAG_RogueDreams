# DocFX — HTML API reference

[DocFX](https://dotnet.github.io/docfx/) turns **compiled assemblies** + **XML documentation** into a static HTML site under `_site/`.

## Prerequisites

1. **Unity** has opened this project at least once so **`.csproj`** files exist next to `Assets/` (e.g. `Assembly-CSharp.csproj`), *or* you use an asmdef-only setup and point `docfx.json` at the correct `.csproj`.
2. **.NET SDK** installed (`dotnet --version`).
3. Install DocFX (one-time):

   ```bash
   dotnet tool install -g docfx
   ```

4. **XML doc comments in the build** — In Unity: *Edit → Project Settings → Player → Other Settings → Api Compatibility Level* and ensure your IDE/csproj generates documentation, or add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to the relevant `.csproj` so summaries appear in HTML. APIs still list without it, but `<summary>` text may be missing.

## Build

From the **`docs/docfx`** directory:

```bash
cd docs/docfx
docfx metadata docfx.json
docfx build docfx.json
```

Then open **`_site/index.html`** in a browser, or:

```bash
docfx docfx.json --serve
```

## Configuration

- **`docfx.json`** — `metadata` reads `Assembly-CSharp.csproj` from the **repository root** (two levels up from `docs/docfx`). If your Unity project uses a different main project name, edit `files` under `metadata → src`.
- **`filterConfig.yml`** — Restricts API pages to namespace `DataSchemas.PackedItem` (adjust or remove to document the whole assembly).

## Git

`_site/` and generated `api/` are ignored via `docs/docfx/.gitignore`. Commit only `docfx.json`, `*.md`, `toc.yml`, and `filterConfig.yml` unless you want to publish built HTML elsewhere.
