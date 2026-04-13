package io.spectrasharp;

import java.io.*;
import java.net.URISyntaxException;
import java.nio.file.*;

/**
 * SpectraSharp Launcher Bootstrap
 *
 * The official Minecraft launcher puts <id>/<id>.jar on the classpath and runs
 * whatever mainClass the version JSON specifies.  This class is that mainClass.
 *
 * It reads the SpectraSharp project path from spectradir.txt, which sits next
 * to SpectraSharp-1.0.jar in the versions folder.  Install.ps1 writes that file.
 *
 * Launch mode:
 *   - RELEASE: "<spectraDir>\SpectraSharp.exe"           (compiled native exe)
 *   - DEBUG:   "dotnet run --project <spectraDir>"       (.csproj found)
 *
 * The Java process stays alive while SpectraSharp runs, then exits with the
 * same exit code so the Minecraft launcher's process model stays intact.
 */
public class Bootstrap {

    public static void main(String[] args) throws Exception {
        System.out.println("[SpectraSharp] Bootstrap starting...");

        // ── Locate our own JAR to find spectradir.txt alongside it ───────────
        Path jarDir = getJarDirectory();
        if (jarDir == null) {
            System.err.println("[SpectraSharp] ERROR: Could not determine JAR location.");
            System.exit(1);
        }

        Path configFile = jarDir.resolve("spectradir.txt");
        if (!Files.exists(configFile)) {
            System.err.println("[SpectraSharp] ERROR: spectradir.txt not found at:");
            System.err.println("               " + configFile);
            System.err.println("               Re-run Tools/Install.ps1 to recreate it.");
            System.exit(1);
        }

        String spectraDir = Files.readString(configFile).strip();
        Path dir    = Paths.get(spectraDir).toAbsolutePath();
        Path exe    = dir.resolve("SpectraSharp.exe");
        Path csproj = dir.resolve("SpectraSharp.csproj");

        System.out.println("[SpectraSharp] Project dir: " + dir);

        ProcessBuilder pb;

        if (Files.exists(exe)) {
            // ── RELEASE: run the compiled native executable ───────────────────
            System.out.println("[SpectraSharp] Mode: RELEASE  ->  " + exe);
            pb = new ProcessBuilder(exe.toString());

        } else if (Files.exists(csproj)) {
            // ── DEBUG: delegate to the .NET CLI ───────────────────────────────
            System.out.println("[SpectraSharp] Mode: DEBUG (dotnet run)  ->  " + dir);
            pb = new ProcessBuilder("dotnet", "run", "--project", dir.toString());

        } else {
            System.err.println("[SpectraSharp] ERROR: Could not find SpectraSharp at:");
            System.err.println("               " + dir);
            System.err.println("               Expected: SpectraSharp.exe  or  SpectraSharp.csproj");
            System.exit(1);
            return;
        }

        pb.directory(dir.toFile());
        pb.inheritIO();   // share console so Raylib + dotnet output is visible

        int exitCode = pb.start().waitFor();
        System.out.println("[SpectraSharp] Exited with code " + exitCode);
        System.exit(exitCode);
    }

    /** Returns the directory containing this class's JAR file, or null on failure. */
    private static Path getJarDirectory() {
        try {
            Path jar = Paths.get(
                Bootstrap.class.getProtectionDomain().getCodeSource().getLocation().toURI()
            );
            return jar.getParent();
        } catch (URISyntaxException | NullPointerException e) {
            return null;
        }
    }
}
