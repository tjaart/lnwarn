# lnwarn


[![.NET](https://github.com/tjaart/lnwarn/actions/workflows/dotnet.yml/badge.svg?event=push)](https://github.com/tjaart/lnwarn/actions/workflows/dotnet.yml)
![Nuget](https://img.shields.io/nuget/dt/:LnWarn.Cmd)

A dotnet tool to count lines and return an error when there are too many.



```
lnwarn --help
Count lines and return non zero when files exceed maximum number of lines.

Usage: LnWarn.Cmd [options]

Options:
  --min-line-length|-ml  Lines that have less characters will not be counted.
                         This will ignore initial whitespace
  --max|-m               If any file has more than this maximum lines, then exit with a non zero exit code
  --include|-i           A list of glob patterns for files to include
  --exclude|-x           A list of glob patterns for files to exclude
  -?|-h|--help           Show help information

```



