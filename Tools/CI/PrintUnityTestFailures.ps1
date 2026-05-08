param(
    [Parameter(Mandatory = $true)]
    [string] $ResultsPath,

    [Parameter(Mandatory = $true)]
    [string] $PlatformName
)

if (-not (Test-Path -LiteralPath $ResultsPath)) {
    Write-Host "[BuildGate] $PlatformName test results file was not created: $ResultsPath"
    exit 0
}

try {
    [xml] $results = Get-Content -LiteralPath $ResultsPath
} catch {
    Write-Host "[BuildGate] Could not parse $PlatformName test results XML: $($_.Exception.Message)"
    exit 0
}

$failedTests = @($results.SelectNodes("//test-case[@result='Failed' or @outcome='Failed']"))
$inconclusiveTests = @($results.SelectNodes("//test-case[@result='Inconclusive' or @outcome='Inconclusive']"))

if ($failedTests.Count -eq 0 -and $inconclusiveTests.Count -eq 0) {
    Write-Host "[BuildGate] All $PlatformName tests passed."
    exit 0
}

Write-Host "[BuildGate] $PlatformName test failures:"

foreach ($test in $failedTests) {
    $testName = $test.fullname
    if ([string]::IsNullOrWhiteSpace($testName)) {
        $testName = $test.name
    }

    $messageNode = $test.SelectSingleNode(".//message")
    $message = ""
    if ($messageNode -ne $null) {
        $message = ($messageNode.InnerText -replace "\s+", " ").Trim()
    }

    if ([string]::IsNullOrWhiteSpace($message)) {
        Write-Host "[BuildGate][FAILED] $testName"
    } else {
        Write-Host "[BuildGate][FAILED] $testName -- $message"
    }
}

foreach ($test in $inconclusiveTests) {
    $testName = $test.fullname
    if ([string]::IsNullOrWhiteSpace($testName)) {
        $testName = $test.name
    }

    Write-Host "[BuildGate][INCONCLUSIVE] $testName"
}
