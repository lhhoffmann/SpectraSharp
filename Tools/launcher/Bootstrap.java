package io.spectrasharp;

import java.io.*;
import java.net.URISyntaxException;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * SpectraSharp Launcher Bootstrap
 *
 * The official launcher puts <id>/<id>.jar on the classpath and runs
 * whatever mainClass the version JSON specifies.  This class is that mainClass.
 *
 * It reads the SpectraSharp project path from spectradir.txt next to the JAR,
 * then launches dotnet run (DEBUG) or SpectraSharp.exe (RELEASE).
 *
 * All output is written to bootstrap.log in the same directory so it is
 * visible even when the launcher swallows stdout.
 */
public class Bootstrap {

    private static PrintStream log;

    public static void main(String[] args) throws Exception {
        // ── Find our own directory ────────────────────────────────────────────
        Path jarDir = getJarDirectory();

        // ── Set up file log immediately ───────────────────────────────────────
        Path logFile = (jarDir != null)
            ? jarDir.resolve("bootstrap.log")
            : Paths.get(System.getProperty("user.home"), "bootstrap.log");

        log = new PrintStream(new FileOutputStream(logFile.toFile(), false), true, "UTF-8");
        tee("=== SpectraSharp Bootstrap ===  " + new SimpleDateFormat("yyyy-MM-dd HH:mm:ss").format(new Date()));
        tee("JAR dir   : " + jarDir);
        tee("CWD       : " + Paths.get("").toAbsolutePath());
        tee("Java      : " + System.getProperty("java.version"));
        tee("Classpath : " + System.getProperty("java.class.path"));

        if (jarDir == null) {
            fatal("Could not determine JAR directory.");
        }

        // ── Read spectradir.txt ───────────────────────────────────────────────
        Path configFile = jarDir.resolve("spectradir.txt");
        tee("Config    : " + configFile + "  exists=" + Files.exists(configFile));

        if (!Files.exists(configFile)) {
            fatal("spectradir.txt not found at: " + configFile);
        }

        String spectraDir = new String(Files.readAllBytes(configFile), StandardCharsets.UTF_8).trim();
        if (spectraDir.startsWith("\uFEFF")) spectraDir = spectraDir.substring(1);
        tee("SpectraDir: " + spectraDir);

        Path dir    = Paths.get(spectraDir).toAbsolutePath();
        Path exe    = dir.resolve("SpectraSharp.exe");
        Path csproj = dir.resolve("SpectraSharp.csproj");
        tee("EXE       : " + exe + "  exists=" + Files.exists(exe));
        tee("CSPROJ    : " + csproj + "  exists=" + Files.exists(csproj));

        // ── Find dotnet.exe ───────────────────────────────────────────────────
        String dotnet = findDotnet();
        tee("dotnet    : " + dotnet);

        ProcessBuilder pb;

        if (Files.exists(exe)) {
            tee("Mode: RELEASE -> " + exe);
            pb = new ProcessBuilder(exe.toString());

        } else if (Files.exists(csproj)) {
            if (dotnet == null) {
                fatal("dotnet not found on PATH or in common install locations.");
            }
            tee("Mode: DEBUG (dotnet run) -> " + dir);
            pb = new ProcessBuilder(dotnet, "run", "--project", dir.toString());

        } else {
            fatal("Neither SpectraSharp.exe nor SpectraSharp.csproj found at: " + dir);
            return;
        }

        pb.directory(dir.toFile());
        pb.inheritIO();

        tee("Launching process...");
        log.flush();

        int exitCode = pb.start().waitFor();
        tee("Process exited with code " + exitCode);
        log.flush();
        System.exit(exitCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void tee(String msg) {
        System.out.println("[SpectraSharp] " + msg);
        if (log != null) log.println("[SpectraSharp] " + msg);
    }

    private static void fatal(String msg) {
        tee("FATAL: " + msg);
        if (log != null) log.flush();
        System.exit(1);
    }

    /** Finds dotnet.exe on PATH or in common Windows install locations. */
    private static String findDotnet() {
        // 1. PATH
        String[] candidates = System.getenv("PATH") != null
            ? System.getenv("PATH").split(File.pathSeparator)
            : new String[0];
        for (String dir : candidates) {
            File f = new File(dir, "dotnet.exe");
            if (f.exists()) return f.getAbsolutePath();
            File f2 = new File(dir, "dotnet");
            if (f2.exists()) return f2.getAbsolutePath();
        }

        // 2. Common install locations on Windows
        String[] commonPaths = {
            "C:\\Program Files\\dotnet\\dotnet.exe",
            System.getenv("LOCALAPPDATA") + "\\Microsoft\\dotnet\\dotnet.exe",
            System.getenv("ProgramFiles") + "\\dotnet\\dotnet.exe"
        };
        for (String p : commonPaths) {
            if (p != null && new File(p).exists()) return p;
        }

        return null;
    }

    /** Returns the directory containing this JAR, using three fallback strategies. */
    private static Path getJarDirectory() {
        // 1. ProtectionDomain
        try {
            Path jar = Paths.get(
                Bootstrap.class.getProtectionDomain().getCodeSource().getLocation().toURI()
            );
            Path parent = jar.getParent();
            if (parent != null) return parent;
        } catch (Exception ignored) {}

        // 2. Scan java.class.path for our JAR name.
        //    Handles both old layout (versions/.../SpectraSharp-1.0.jar)
        //    and new library layout (.../io/spectrasharp/bootstrap/1.0/bootstrap-1.0.jar).
        String cp = System.getProperty("java.class.path", "");
        for (String entry : cp.split(File.pathSeparator)) {
            String lower = entry.toLowerCase();
            if (lower.contains("spectrasharp-1.0") || lower.contains("bootstrap-1.0")) {
                Path parent = Paths.get(entry).toAbsolutePath().getParent();
                if (parent != null) return parent;
            }
        }

        // 3. Working directory (launcher sets it to the versions folder for old versions)
        return Paths.get("").toAbsolutePath();
    }
}
