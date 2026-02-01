for /l %%i in (1,1,5) do (
    start "Instance %%i" cmd /c "dotnet run"
)