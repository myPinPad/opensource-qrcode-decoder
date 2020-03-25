$ErrorActionPreference = "Stop"

function DownloadLatestRelease([string] $downloadLocation) {
    $token = $env:COREBUILD_DOWNLOAD_TOKEN
    $reposUri = "https://api.github.com/repos/myPinPad/coreBuild"

    try {
        $tokenBase64 = [System.Convert]::ToBase64String([char[]]$token)
        $headers = @{
            Authorization = "Basic $tokenBase64"
        }

        $response = Invoke-RestMethod -Method Get -Headers $headers "$reposUri/tags"
        if ($response.Length -eq 0) {
            Write-Host "No tagged releases found."
            exit -1
        }

        Write-Host "Found release '$($response[0].name)', downloading ..."
        Invoke-WebRequest -Headers $headers $response[0].zipball_url -OutFile $downloadLocation
    }
    catch {
        $exceptionMessage = $_.Exception.Message
        Write-Host "Failed to download '$url': $exceptionMessage"
        exit -1
    }
}

# required for GitHub
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Push-Location $PSScriptRoot
$env:REPO_FOLDER = $PSScriptRoot

$buildFolder = ".build"
$buildFile = "$buildFolder/CoreBuild.ps1"

if (!(Test-Path $buildFolder)) {
    Write-Host "Looking for latest Core Build release ..."

    $tempFolder = $env:TEMP + "/coreBuild-" + [guid]::NewGuid()
    New-Item -Path "$tempFolder" -Type directory | Out-Null

    $localZipFile = "$tempFolder/corebuild.zip"

    DownloadLatestRelease $localZipFile

    Expand-Archive $localZipFile -DestinationPath $tempFolder

    New-Item -Path "$buildFolder" -Type directory | Out-Null
    Copy-Item "$tempFolder/**/build/*" $buildFolder -Recurse

    # Cleanup
    if (Test-Path $tempFolder) {
        Remove-Item -Recurse -Force $tempFolder
    }
}

& $buildFile @args

Pop-Location
