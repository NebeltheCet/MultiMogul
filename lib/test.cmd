@echo off
echo ^<ItemGroup^> > xml_block.txt

for %%f in (*.dll) do (
    echo     ^<Reference Include="%%~nf"^> >> xml_block.txt
    echo       ^<HintPath^>..\lib\%%~nxf^</HintPath^> >> xml_block.txt
    echo     ^</Reference^> >> xml_block.txt
)

echo ^</ItemGroup^> >> xml_block.txt
echo Done! Open xml_block.txt
