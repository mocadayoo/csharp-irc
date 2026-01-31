for /l %%i in (1,1,20) do (
    start "Instance %%i" cmd /c "dotnet run"
)