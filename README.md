MDLReaderWriter
===============

Example usage to convert an allegiance model and its texture to a lightwave Obj

-- This command will create the obj model

mdld.exe -file="C:\Alleg\Artwork\acs04.mdl" -out=acs04.obj -format=obj

-- This command will create the .mtl file for the obj file, and also create a png with the same name

mdld.exe -file="C:\Alleg\Artwork\acs04bmp.mdl" -out=acs04bmp.mtl -format=mtl


Another example

mdld.exe -file="C:\Alleg\Artwork\fig03.mdl" -out=fig03.obj -format=obj

