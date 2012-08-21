ResX Resource Editor Addin for MonoDevelop
==========================================

Enables editing ResX format resource files within MonoDevelop. This is an alpha version created as part of the Google Summer of Code program along with various supporting updates to the mono framework and MonoDevelop projects. (1)

Grid style editor for embedded string resources.
------------------------------------------------
* Display, updating, adding and deletion of strings.
* Displays the corresponding value from the default culture file if available.
* Supports sorting and filtering by name, value, default value and comment.

Icon based editing of non string resources.
-------------------------------------------
* Categorised into Image, Icon, Audio sections.
* Additional section for resources linking to files of other types.
* Additional section for resources embedded in ResX of other types.
* Non Image, Icon or Audio files can be set to String or Byte[] type within the editor.
* Encoding can be set for text files.
* Supports adding new Image / Icon / Audio / String or Binary type resources.
* Can export embedded Image / Icon / Audio resources to become file links either in the root Resources folder of a project or location of the users choosing if not.
* Can import Image / Icon / Audio resource files to become embedded. (2)
* Supports deletion of resources and editing their properties.

Additional
----------
* Support for metadata resources stored in ResX files.
* Can enable generation of internal or public class to provide strongly typed access to resources. (3)
* Can be used with the Generate Local Resources Wizard to enhance localisation for ASP.NET web apps. (4)

Notes
-------
(1) The addin requires a patched version of Manged.Windows.Forms assembly submitted alongside this project.

(2) Vista style icons with 256x256px images will require the patched version of the System.Drawing assembly submitted alongside this project.

(3) Code generation will require a patched version of the System.Design assembly submitted alongside this project.

(4) The Generate Local Resources wizard requires a patched version of MonoDevelop submitted alongside this project
