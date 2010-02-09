#!/bin/sh

gmcs -d:DEBUG -debug+ -resource:Resources/Logo.png -resource:Resources/LogoShadow.png -resource:Resources/ListItem.png -resource:Resources/SelectedListItem.png -resource:Resources/Scale.png -resource:Resources/TextLine.png -resource:Resources/Collapsed.png -resource:Resources/Expanded.png -resource:Resources/Network.png -resource:Resources/Waypoint.png -resource:Resources/Connection.png -resource:Resources/TitleBackground.png -resource:Resources/Path.png -resource:Resources/PathWhite.png -resource:Resources/_Help.png -resource:Resources/Tag.png -resource:Resources/UnfocusedSelectedListItem.png -r:/Applications/Unity/Unity.app/Contents/Frameworks/UnityEngine.dll,/Applications/Unity/Unity.app/Contents/Frameworks/UnityEditor.dll -target:library -out:Path.dll Source/*.cs

mv -f Path.dll ../../Assets/Path/
rsync -r Package/ ../../Assets/
