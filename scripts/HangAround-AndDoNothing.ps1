$tc = [Microsoft.ApplicationInsights.TelemetryClient]::New()
$tc.InstrumentationKey = "148b913b-2236-43a1-a600-b396d250c976"

$tc.TrackEvent("Going to sleep")
Start-Sleep -Seconds 300
$tc.TrackEvent("Waking up")

Exit
