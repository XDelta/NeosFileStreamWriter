# NeosFileStreamWriter
A Plugin for [Neos VR](https://neos.com/) that adds a FileStreamWriter LogiX node.
This is an alternative option to the WriteTextToFile Node that allows keeping the file handle open until explicitly closed to avoid repeated file open/read operations.

To use this node you'll need to be in an Unsafe world and enable the ShowExperimental option on a node browser.
It can then be found under the `/Experimental/` Folder

![image](https://user-images.githubusercontent.com/7883807/220058135-108b2f65-bd0b-44ce-8407-419fef5eb36b.png)
