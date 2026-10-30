# Build an isolated 0.3.7 navigation candidate

The default builder pins upstream commit `f76e437` and navigation commit `294c667`. It reads the published `Hermes-Practice-v0.3.0.zip`, verifies its pinned SHA-256, then applies the single cumulative `Hermes-Campaign-v0.3.7-source.patch` exactly once and verifies SHA-256 `5f59faa0a8220abf9a5119b76fb98caac8e4e73c189cc7e4d2f05948b80b8770`.

Do not stack 0.3.6, coast, tests, console, supply, epoch, or QA patches on top of 0.3.7. The packet includes its host-owned source, tests, and documentation. The builder extracts and modifies only a new output directory, overlays tracked navigation source files from the explicit navigation ref, and can compile all plugin sources as C# 7.3 against a local KSP `Data/Managed` directory. It does not install the plugin, access saves or a server, or change the checkout.

Examples (from repository root):

```sh
python3 tools/build_navigation_candidate.py --repo . --output /tmp/megacrew-037-prepare --prepare-only
python3 tools/build_navigation_candidate.py --repo . --output /tmp/megacrew-037-build --managed /path/to/KSP_Data/Managed --csc /path/to/dotnet/sdk/Roslyn/bincore/csc.dll
```

An existing output directory is rejected. The manifest records exact patch sequence and hashes, source refs, output hashes, and compile inputs. Tests include unsafe ZIP members, traversal/symlink defense, hash mismatch, pre-existing destinations, relative output paths, and a synthetic two-patch dependency proving ordered cumulative application.
