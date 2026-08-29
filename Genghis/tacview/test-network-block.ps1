$AppName = "Launcher64.exe" # The executable name for the CloD Server
$BlockTime = 30       # Seconds to stay disconnected

Write-Host "Blocking all network traffic to $AppName for $BlockTime seconds..." -ForegroundColor Yellow

# Create Outbound Block
New-NetFirewallRule -DisplayName "TEST_BLOCK_OUT" -Direction Outbound -Program "*$AppName.exe" -Action Block -Enabled True | Outbound
# Create Inbound Block
New-NetFirewallRule -DisplayName "TEST_BLOCK_IN" -Direction Inbound -Program "*$AppName.exe" -Action Block -Enabled True | Outbound

# Count down the blackout window
for ($i = $BlockTime; $i -gt 0; $i--) {
    Write-Host "Restoring connection in $i seconds... " -NoNewline
    Start-Sleep -Seconds 1
}

# Remove the block rules entirely to restore the connection
Remove-NetFirewallRule -DisplayName "TEST_BLOCK_OUT" -ErrorAction SilentlyContinue
Remove-NetFirewallRule -DisplayName "TEST_BLOCK_IN" -ErrorAction SilentlyContinue

Write-Host "`nNetwork connection restored!" -ForegroundColor Green