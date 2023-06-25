#!/bin/bash

dotnet tool uninstall --global LnWarn.Cmd
dotnet pack
dotnet tool install --global --add-source ./nupkg LnWarn.Cmd