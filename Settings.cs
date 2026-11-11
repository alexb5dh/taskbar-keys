using static TaskbarKeys.Native;

namespace TaskbarKeys;

static unsafe class Settings
{
    const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run", RunValue = "TaskbarKeys";
    const uint REG_SZ = 1, RRF_RT_REG_SZ = 0x2, RRF_RT_REG_DWORD = 0x10;

    static readonly nint CurrentUser = unchecked((nint)(int)0x80000001);

    public static bool IsLightTheme()
    {
        uint value = 0, size = sizeof(uint);
        return RegGetValueW(CurrentUser, PersonalizeKey, "SystemUsesLightTheme", RRF_RT_REG_DWORD, null, &value, &size) == 0 && value != 0;
    }

    public static bool RunsAtStartup()
    {
        uint size = 0;
        return RegGetValueW(CurrentUser, RunKey, RunValue, RRF_RT_REG_SZ, null, null, &size) == 0;
    }

    public static void SetRunAtStartup(bool enabled, string command)
    {
        if (!enabled)
        {
            RegDeleteKeyValueW(CurrentUser, RunKey, RunValue);
            return;
        }

        fixed (char* data = command)
            RegSetKeyValueW(CurrentUser, RunKey, RunValue, REG_SZ, data, (uint)(command.Length + 1) * sizeof(char));
    }
}
