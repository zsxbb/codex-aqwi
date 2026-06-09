$envPath = Join-Path $PSScriptRoot "..\.env"
$ip = "46.250.228.209"
$port = 3000
$activationKey = "codex_secret_2026"
$encKey = "CodexVoiceShield2026"

if (Test-Path $envPath) {
    $envVars = @{}
    Get-Content $envPath | Foreach-Object {
        if ($_ -match "^(.*?)=(.*)$") {
            $envVars[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    if ($envVars.ContainsKey("SIGNALING_SERVER_IP")) { $ip = $envVars["SIGNALING_SERVER_IP"] }
    if ($envVars.ContainsKey("SIGNALING_SERVER_PORT")) { $port = [int]$envVars["SIGNALING_SERVER_PORT"] }
    if ($envVars.ContainsKey("ACTIVATION_HASH_KEY")) { $activationKey = $envVars["ACTIVATION_HASH_KEY"] }
    if ($envVars.ContainsKey("VOICE_ENCRYPTION_KEY")) { $encKey = $envVars["VOICE_ENCRYPTION_KEY"] }
}

$configText = @"
public static class Config
{
    public const string SignalingServerIP = "$ip";
    public const int SignalingServerPort = $port;
    public const string ActivationHashKey = "$activationKey";
    public const string VoiceEncryptionKey = "$encKey";
}
"@

$outputPath = Join-Path $PSScriptRoot "Config.g.cs"
Set-Content -Path $outputPath -Value $configText -Encoding UTF8
