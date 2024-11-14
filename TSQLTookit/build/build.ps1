using module ./antlr.psm1

$javaTarget = 11            # Minimum Java version required
$antlrVersion = "4.13.2"    # Antlr4 version required
$rootPath = Split-Path $PSScriptRoot -Parent

[Antlr]::new($rootPath, $antlrVersion, $javaTarget).build()