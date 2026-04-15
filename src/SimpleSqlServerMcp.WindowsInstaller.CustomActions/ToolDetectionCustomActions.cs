using WixToolset.Dtf.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.CustomActions;

public static class ToolDetectionCustomActions
{
    [CustomAction]
    public static ActionResult DetectInstalledTools(Session session)
    {
        try
        {
            if (session["TOOL_DETECTION_INITIALIZED"] == "1")
            {
                session.Log("Tool detection already initialized. Skipping.");
                return ActionResult.Success;
            }

            var detector = new WindowsToolInstallationDetector();

            foreach (var tool in ToolDetectionCatalog.All)
            {
                if (detector.IsDetected(tool))
                {
                    session[tool.InstallProperty] = "1";
                    session.Log($"Detected Windows tool for `{tool.InstallProperty}`.");
                }
            }

            session["TOOL_DETECTION_INITIALIZED"] = "1";
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            session.Log("Tool detection custom action failed: " + exception);
            return ActionResult.Success;
        }
    }
}
