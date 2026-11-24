# TuxDDS

## TODO

- Add DDS export in another format
- Add CLI version
- Add a thumbnailer
- Add about dialog
- Add unit testing
- Adjust code to styleguide
- Create README

## CLI Usage

```bash
tuxdds <command> [options]
```

### Commands

| Command | Description |
| --- | --- |
| `info <file.dds>` | Display metadata (dimensions, format, bits per pixel/color) of a DDS file |
| `convert [options]` | Convert a single DDS file to PNG or JPG |
| `batch [options]` | Batch convert DDS files inside a directory |
| `log` | Print the TuxDDS log file |
| `about`, `version`, `-v`, `--version` | Show version, copyright, and license information |
| `help [command]`, `-h`, `--help` | Show general help or detailed options for a command |

### Single Image Conversion (`convert`)

```bash
tuxdds convert -i <input.dds> -o <output> [-f png|jpg]
```

- `-i`, `--input <file.dds>`: Input DDS file path (required).
- `-o`, `--output <path>`: Output file path or target directory (required).
- `-f`, `--format <png|jpg>`: Target image format (`png` or `jpg`/`jpeg`). Optional — inferred from the output file extension if omitted, defaulting to `png`.

### Batch Conversion (`batch`)

```bash
tuxdds batch -i <input-dir> -o <output-dir> [-f png|jpg] [-r] [-k] [-a <suffix> | -n <name> | --keep-name]
```

- `-i`, `--input <dir>`: Input directory containing DDS files (required).
- `-o`, `--output <dir>`: Target output directory (required, created automatically if needed).
- `-f`, `--format <png|jpg>`: Target image format (default: `png`).
- `-r`, `--recursive`: Search subdirectories recursively.
- `-k`, `--keep-structure`, `--keep-folders`: Preserve the relative subdirectory structure in the output folder.
- `--keep-name`: Keep original file names (default).
- `-a`, `--append <suffix>`: Append `-<suffix>` to exported file names (`<filename>-<suffix>.<ext>`).
- `-n`, `--rename`, `--batch-rename <name>`: Rename exported files sequentially (`<name>-1.<ext>`, `<name>-2.<ext>`, ...).

## Supported DDS Formats

These file formats have been tested and work as expected.

- A4B4G4R4_UNORM
- A8_UNORM
- AYUV
- B4G4R4A4_UNORM
- B5G5R5A1_UNORM
- B5G6R5_UNORM
- B8G8R8A8_UNORM
- B8G8R8A8_UNORM_SRGB
- B8G8R8X8_UNORM
- B8G8R8X8_UNORM_SRGB
- BC1_UNORM
- BC1_UNORM_SRGB
- BC2_UNORM
- BC2_UNORM_SRGB
- BC3_UNORM
- BC3_UNORM_SRGB
- BC4_SNORM
- BC4_UNORM
- BC5_SNORM
- BC5_UNORM
- BC6H_SF16
- BC6H_UF16
- BC7_UNORM
- BC7_UNORM_SRGB
- G8R8_G8B8_UNORM
- R10G10B10A2_UINT
- R10G10B10A2_UNORM
- R10G10B10_XR_BIAS_A2_UNORM
- R11G11B10_FLOAT
- R16_FLOAT
- R16G16B16A16_FLOAT
- R16G16B16A16_SINT
- R16G16B16A16_SNORM
- R16G16B16A16_UINT
- R16G16B16A16_UNORM
- R16G16_FLOAT
- R16G16_SINT
- R16G16_SNORM
- R16G16_UINT
- R16G16_UNORM
- R16_SINT
- R16_SNORM
- R16_UINT
- R16_UNORM
- R32_FLOAT
- R32G32B32A32_FLOAT
- R32G32B32A32_SINT
- R32G32B32A32_UINT
- R32G32B32_FLOAT
- R32G32B32_SINT
- R32G32B32_UINT
- R32G32_FLOAT
- R32G32_SINT
- R32G32_UINT
- R32_SINT
- R32_UINT
- R8G8B8A8_SINT
- R8G8B8A8_SNORM
- R8G8B8A8_UINT
- R8G8B8A8_UNORM
- R8G8B8A8_UNORM_SRGB
- R8G8_B8G8_UNORM
- R8G8_SINT
- R8G8_SNORM
- R8G8_UINT
- R8G8_UNORM
- R8_SINT
- R8_SNORM
- R8_UINT
- R8_UNORM
- R9G9B9E5_SHAREDEXP
- Y210
- Y216
- Y410
- Y416
- YUY2
