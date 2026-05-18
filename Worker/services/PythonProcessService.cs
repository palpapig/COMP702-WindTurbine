using System.Diagnostics;
namespace COMP702_WindTurbine.services;

public class PythonProcessService
{
    private Process? pythonProcess;

    public void Start()
    {


        Console.WriteLine("###############################################################################################");
        var projectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")
        );

        var pythonExe = Path.Combine(
            projectRoot, "faultDetection_service", ".venv", "Scripts", "python.exe"
        );

        var workingDir = Path.Combine(
            projectRoot, "faultDetection_service"
        );

        if (!File.Exists(pythonExe))
        {
            Console.WriteLine("Python executable not found!");
            return;
        }

        pythonProcess = new Process();

        pythonProcess.StartInfo.FileName = pythonExe;
        pythonProcess.StartInfo.Arguments = "-m uvicorn app.main:app --host 127.0.0.1 --port 8000";
        pythonProcess.StartInfo.WorkingDirectory = workingDir;

        pythonProcess.StartInfo.UseShellExecute = true;
        pythonProcess.StartInfo.CreateNoWindow = false;

        pythonProcess.Start();
    }

    public void Stop()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
        }
    }
}