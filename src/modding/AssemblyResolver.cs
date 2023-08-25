using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using OpenMine.Extensions;

public static class AssemblyResolver
{
    internal static void Hook(params string[] folders)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            // Check if the requested assembly is part of the loaded assemblies
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (loadedAssembly != null)
                return loadedAssembly;

            // This resolver is called when a loaded control tries to load a generated XmlSerializer - We need to discard it.
            // http://connect.microsoft.com/VisualStudio/feedback/details/88566/bindingfailure-an-assembly-failed-to-load-while-using-xmlserialization

            var n = new AssemblyName(args.Name);

            if (n.Name.EndsWith(".xmlserializers", StringComparison.OrdinalIgnoreCase))
                return null;

            // http://stackoverflow.com/questions/4368201/appdomain-currentdomain-assemblyresolve-asking-for-a-appname-resources-assembl

            if (n.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            string assembly = null;
            // Get execution folder to use as base folder
            var rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            // Find the corresponding assembly file
            foreach (var dir in folders)
            {
                assembly = new[] { "*.dll", "*.exe" }.SelectMany(g => Directory.EnumerateFiles(Path.Combine(rootFolder, dir), g)).FirstOrDefault(f =>
                {
                    try
                    {
                        return n.Name.Equals(AssemblyName.GetAssemblyName(f).Name,
                            StringComparison.OrdinalIgnoreCase);
                    }
                    catch (BadImageFormatException)
                    {
                        return false; /* Bypass assembly is not a .net exe */
                    }
                    catch (Exception ex)
                    {
                        // Logging etc here
                        throw;
                    }
                });

                if (assembly != null)
                    return Assembly.LoadFrom(assembly);
            }

            // More logging for failure here
            return null;
        };
    }

    internal static void EmbeddedLibraryHook(Assembly assemblyToLoadFrom)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            try
            {
                var libraries = Array.FindAll(assemblyToLoadFrom.GetManifestResourceNames(), r => r.EndsWith(".dll"));
                foreach (var library in libraries)
                {
                    using (Stream stream = assemblyToLoadFrom.GetManifestResourceStream(library))
                    {
                        return Assembly.Load(stream.ReadAllBytes());
                    }
                }
            }
            catch (Exception ex)
            {
                GD.Print("Error dynamically loading dll: " + args.Name, ex);
            }
            return null;
        };
    }
}
