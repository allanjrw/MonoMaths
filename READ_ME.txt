DESCRIPTION
"MonoMaths" is a small but powerful mathematical application, quickly loaded, and with relatively simple syntax. It can be run as a simple calculator for quick answers ("What is the square root of 5?"), yet has no problem handling large programs (my source codes are typically around 100K). Some basic facts:
*  It runs on C# using Mono, the Linux equivalent of the .NET platform. It currently uses Mono 4.8.0, but should run on earlier and later versions of Mono. (For graphics, Mono uses GTK and Cairo.)
*  The general syntax is a subset of the syntax typically used by C and C-like languages. (There are a few additional syntactical features for specialized tasks.)
*  The language does not have classes, nor does it have the equivalent of "structures" in C. It also does not allow the use of pointers.
*  There are many system functions (currently around 450) which can be simply called on for particular computational tasks.
*  Array operations are carried out on an element-by-element basis, without reference to Linear Algebra. However functions exist which enable e.g. formal matrix multiplication to be carried out.
*  Programs are run in the IDE of the application rather than in a terminal. The IDE has a large number of features to assist with this, including context-sensitive help.

"MonoMaths" was originally developed to meet my needs while studying for an MSc in engineering; it has continued to grow as I have been working towards a computation-intensive PhD in engineering. It was initially developed using C# .NET under the Windows operating system (under its earlier name "MiniMaths"); once Linux became my preferred operating medium for all applications I switched to C# running on Mono, and so changed its name to the above.

As the application was developed for my personal use it has some idiosyncrasies in presentation and style; but to the best of my ability the actual computation is standard and reliable.

There are copious "Help" notes available from within the application at run time, both as texts from a menu and as context-sensitive information. If you want to read this information separately without running the program, you will find it in two lightly-formatted (but completely readable) text files supplied in folder "For_Working_Directory":  "Help.txt" and "Hints.txt".  The first starts with an introduction to the language and its syntax, which may be helpful up front. 

INSTALLATION
All the necessary C# source and project files are included, ready for compilation. It would also be possible to directly use the compiled Mono program "MonoMaths.exe" (found in "MonoMaths/MonoMaths/bin/Debug/"). Either way, the following folder arrangements are recommended for proper functioning:
*  On your computer, set apart a working directory in your "home" folder for MonoMaths, then transfer to it all of the files supplied here in "For_Working_Directory". Place the compiled program "MonoMaths.exe" into this new working directory.
*  Run the application using the instruction (in a terminal, or a launchpad): "mono  My_working_directory/MonoMaths.exe".
*  Once the IDE is displayed, choose menu item "Help | Overview" to get some most basic understanding of the application.




