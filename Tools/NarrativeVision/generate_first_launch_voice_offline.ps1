param(
    [string]$FfmpegPath = "ffmpeg",
    [string]$CatalogPath = "Assets/Game/Data/Narrative/FirstLaunch/first_launch_english_text_catalog.json",
    [string]$VoiceRoot = "Assets/Game/Audio/Narrative/FirstLaunch/Voice",
    [string]$ManifestPath = "Assets/Game/Audio/Narrative/FirstLaunch/first_launch_temp_voice_manifest.json"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Speech

$profiles = @{
    RADIO = @{ Voice = "Microsoft David Desktop"; Rate = 3; Processing = "dispatch-radio" }
    DALIA = @{ Voice = "Microsoft Hazel Desktop"; Rate = 1; Processing = "field-comms" }
    SAMIRA = @{ Voice = "Microsoft Zira Desktop"; Rate = 0; Processing = "field-comms" }
    ARIA = @{ Voice = "Microsoft Zira Desktop"; Rate = 1; Processing = "aria-clean" }
    COMMANDER = @{ Voice = "Microsoft David Desktop"; Rate = 1; Processing = "commander-clean" }
}

$maxDurations = @{
    p02_radio = 7.45
    p03_radio = 8.45
    p04_dalia = 11.25
    p04_samira = 11.25
    p05_aria = 9.25
    p06_aria = 9.25
    p07_aria = 9.25
    p09_aria = 7.25
    p10_aria = 7.25
    p11_dalia = 8.25
    p12_samira = 8.25
    p13_aria = 7.25
    p14_commander = 7.25
    p15_dalia = 9.25
    p16_aria = 9.25
    p17_dalia = 7.25
    p18_aria = 9.25
}

$filters = @{
    "dispatch-radio" = "highpass=f=320,lowpass=f=3300,acompressor=threshold=0.12:ratio=3:attack=5:release=80:makeup=1.8,loudnorm=I=-17:LRA=5:TP=-2"
    "field-comms" = "highpass=f=220,lowpass=f=4300,acompressor=threshold=0.15:ratio=2.5:attack=8:release=100:makeup=1.5,loudnorm=I=-18:LRA=6:TP=-2"
    "aria-clean" = "asetrate=45864,aresample=44100,atempo=0.9615,loudnorm=I=-18:LRA=7:TP=-2"
    "commander-clean" = "asetrate=42336,aresample=44100,atempo=1.0417,loudnorm=I=-18:LRA=7:TP=-2"
}

function Get-WaveDuration([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    $reader = [System.IO.BinaryReader]::new($stream)
    try {
        if ([System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4)) -ne "RIFF") {
            throw "Not a RIFF WAV: $Path"
        }
        $reader.ReadInt32() | Out-Null
        if ([System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4)) -ne "WAVE") {
            throw "Not a WAVE file: $Path"
        }

        $byteRate = 0
        $dataBytes = 0
        while ($stream.Position -le $stream.Length - 8) {
            $chunkId = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
            $chunkSize = $reader.ReadInt32()
            $chunkStart = $stream.Position
            if ($chunkId -eq "fmt ") {
                $reader.ReadInt16() | Out-Null
                $reader.ReadInt16() | Out-Null
                $reader.ReadInt32() | Out-Null
                $byteRate = $reader.ReadInt32()
            } elseif ($chunkId -eq "data") {
                $dataBytes = $chunkSize
            }
            $stream.Position = $chunkStart + $chunkSize + ($chunkSize % 2)
        }
        if ($byteRate -le 0 -or $dataBytes -le 0) {
            throw "Missing WAV format or data chunk: $Path"
        }
        return $dataBytes / $byteRate
    } finally {
        $reader.Dispose()
    }
}

function Get-SpokenText([string]$Text) {
    return $Text.Replace("JRC", "J R C").Replace("ARIA", "Aria")
}

$catalog = Get-Content -LiteralPath $CatalogPath -Raw | ConvertFrom-Json
if ($catalog.lines.Count -ne 17) {
    throw "Expected 17 FirstLaunch dialogue lines, found $($catalog.lines.Count)."
}

$resolvedFfmpeg = (Get-Command $FfmpegPath -ErrorAction Stop).Source
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("warline-first-launch-voice-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $work | Out-Null
$clips = [System.Collections.Generic.List[object]]::new()

try {
    foreach ($line in $catalog.lines) {
        $profile = $profiles[$line.speaker]
        if ($null -eq $profile) {
            throw "No offline voice profile for speaker $($line.speaker)."
        }

        $rawPath = Join-Path $work "$($line.lineId)-raw.wav"
        $processedPath = Join-Path $work "$($line.lineId).wav"
        $synth = [System.Speech.Synthesis.SpeechSynthesizer]::new()
        try {
            $synth.SelectVoice($profile.Voice)
            $synth.Rate = $profile.Rate
            $format = [System.Speech.AudioFormat.SpeechAudioFormatInfo]::new(
                44100,
                [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,
                [System.Speech.AudioFormat.AudioChannel]::Mono)
            $synth.SetOutputToWaveFile($rawPath, $format)
            $synth.Speak((Get-SpokenText $line.text))
        } finally {
            $synth.Dispose()
        }

        $processing = $profile.Processing
        $filter = $filters[$processing]
        if ([string]::IsNullOrWhiteSpace($filter)) {
            $filter = "loudnorm=I=-18:LRA=7:TP=-2"
        }
        & $resolvedFfmpeg -nostdin -hide_banner -loglevel error -y -i $rawPath -af $filter -ac 1 -ar 44100 -c:a pcm_s16le $processedPath
        if ($LASTEXITCODE -ne 0) {
            throw "ffmpeg failed for $($line.lineId)."
        }

        $duration = Get-WaveDuration $processedPath
        $maximum = [double]$maxDurations[$line.lineId]
        if ($duration -gt $maximum) {
            throw "$($line.lineId) is $($duration.ToString('0.00'))s, exceeding its $($maximum.ToString('0.00'))s dialogue window."
        }

        New-Item -ItemType Directory -Force -Path $VoiceRoot | Out-Null
        $destination = Join-Path $VoiceRoot "$($line.lineId).wav"
        Copy-Item -LiteralPath $processedPath -Destination $destination -Force
        $relativeAssetPath = ($destination -replace "\\", "/")
        $clips.Add([ordered]@{
            clipId = $line.lineId
            speaker = $line.speaker
            voice = $profile.Voice
            rate = $profile.Rate
            processing = $processing
            durationSeconds = [math]::Round($duration, 3)
            sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
            assetPath = $relativeAssetPath
        })
        Write-Output "$($line.lineId): $($duration.ToString('0.000'))s $($profile.Voice) [$processing]"
    }

    $manifest = [ordered]@{
        schemaVersion = 2
        generatedAtUtc = [DateTime]::UtcNow.ToString("o")
        assetStatus = "TEMP_INTERNAL_ONLY_DISTRIBUTION_RIGHTS_UNVERIFIED"
        provider = "Microsoft Windows offline SAPI"
        usage = "Offline imported AudioClip assets for internal development and review only"
        shippingApproved = $false
        runtimeNetworkTts = $false
        sourceCatalog = ($CatalogPath -replace "\\", "/")
        radioTreatment = "David dispatch voice with narrow-band command-radio processing; Dalia and Samira use lighter field-comms processing."
        clips = $clips
    }
    $json = $manifest | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText($ManifestPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
} finally {
    if (Test-Path -LiteralPath $work) {
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
