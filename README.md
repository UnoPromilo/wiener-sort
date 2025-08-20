# WienerSort

A .NET 9 toolset for generating and sorting large files.

---

## How to Build

1. Clone the repository:

```bash
git clone https://github.com/UnoPromilo/wiener-sort.git
cd wiener-sort
```

2. Publish the app in Release mode with Native AOT:

```bash
dotnet publish \
  -c Release \
  -p:PublishAot=true \
  -p:StripSymbols=true \
  -p:InvariantGlobalization=true \
  -o ./out \
  ./WienerSort.Console/WienerSort.Console.csproj
```

## How to Run

### Generator

1. Navigate to the output directory:

```bash
cd ./out
```

2. Run the generator:

```bash
./WienerSort.Console generate -o <output-file-path> <target-size-in-mb>
```

This creates a binary file of approximately the specified size.

### Sorter

1. Navigate to the output directory:

```bash
cd ./out
```

2. Run the sorter:

```bash
./WienerSort.Console sort -i <input-file-path> -o <output-file-path>
```

This sorts the input binary file and writes the result to the output path.

## Tip: Use --help

All commands support --help. It’s highly recommended to run it to see all available options:

```bash
./WienerSort.Console --help
./WienerSort.Console generate --help
./WienerSort.Console sort --help
```

This will show you detailed usage instructions and optional parameters.

## Tip: Use --help

If you want to sort large files, try to use bigger chunk size, check `./WienerSort.Console sort --help` for details.

## TODO
- Add multilayer chunk merging so it will work better with bigger files
- Add unit tests
- Add benchmarks