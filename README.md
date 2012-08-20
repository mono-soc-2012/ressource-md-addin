ResX Resource Editor Addin for MonoDevelop
==========================================

Enables editing ResX format resource files within MonoDevelop (0)

Supports
--------

* Display, updating, adding and deleting of embedded string resources in a grid format
* Display of the corresponding default culture value for string resources
* Sorting of string resource grid
* Filtering of string resource grid

* Display of non string resources as icons categorised into Image, Icon, Audio sections 
* Additional section for resources linking to files of other types
* Additonal section for resources embedded in resx of other types

* Switching resources of external files between String or Byte[]
* Setting encoding of external text files

* Adding new Image / Icon / Audio / String or Binary type resources
* Exporting of embedded Image / Icon / Audio resources to become file links, to a root Resources folder if part of a project or location of the users choosing if not
* Importing of Image / Icon / Audio resources files to become embedded resources (1)
* Deletion of resources
* Editing properties of resources

* Opening and saving resource files without instantiating objects (as long as they are not metadata resources)

* Support for metadata resources stored in resx files (3)

* Enable generation of internal or public class to provide strongly typed access to resources (2)

* Can be used with the Generate Local Resources Wizard to enhance localisation for ASP.NET web apps (4)

Notes
-----
(0) Resource files containing anything other than string resources will required patched version of System.Resources namespace provided as part of this project.

(1) Icons with png portions will require the patched version of System.Drawing.Icon class submitted as part of this project.

(2) Will require patched version of System.Design namespace submitted as part of this project

(3) The types for metadata objects must be from assemblies already loaded into MonoDevelop

(4) Requires a patched version of MonoDevelop submitted as part of this project
