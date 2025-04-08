param (
    [string]$environment_file
)

Get-Content $environment_file | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)\s*=\s*(.*)$') {
        $key = $matches[1].Trim()
        $val = $matches[2].Trim().Trim('"').Trim("'")
        if ($val[0] -ne '$'){
            Set-Item -Path Env:\$key -Value $val
        }
        else{
            $sub_val = $val.substring(1)
            Set-Item -Path Env:\$key -Value (Get-Item Env:\$sub_val).Value
        }
    }
}