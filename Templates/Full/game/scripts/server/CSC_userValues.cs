//-----------------------------------------------------------------------------
// Copyright (c) 2018 Twisted Jenius LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------


GlobalActionMap.bind(keyboard, "alt s", toggleCSCEditor);

//Speed is not a 0.0 to 1.0 value even though some Torque documents say that it is
$CSC_Speed = 5;
//Default path name
$CSC_Name = "Path01";
//Default client
$CSC_Client = "LocalClientConnection"; //ClientGroup.getObject(0);
//Default shake datablock
$CSC_Datablock = "PathedCam";
$pref::Player::defaultFov = 65;
//Time in MS per 90 degree change, 0 to 2000
$pref::Player::zoomSpeed = 1;

//Contains camera shaking related data, this is the default datablock
datablock PathCameraData($CSC_Datablock)
{
    //Make sure all 3 values camShakeFreq are different from each other
    camShakeFreq = "10.0 11.0 9.0";
    camShakeAmp = "15.0 15.0 15.0";
    camShakeFalloff = 10;
};

//Example of a second PathCameraData datablock, note the unique name below
datablock PathCameraData(ExamplePathCamData01)
{
    //Use whatever numbers look good to you
    camShakeFreq = "15.6 8.9 12.1";
    camShakeAmp = "14.0 17.6 22.4";
    camShakeFalloff = 4;
};

//This function is called when the cut scene is completely over
function PathCamera::onEnd(%this, %client)
{
    //You can add your own script to execute here
    //%this is the pathCamera itself, it will be deleted next time a cut scene starts
    //%client is the client who was using the camera, by default its the same
    //as the value of $CSC_Client
}


//-----------------------------------------------------------------------------
//Execute Files
//-----------------------------------------------------------------------------

//Scripts files
exec("./CSC_pathManagement.cs");
exec("./CSC_pathCamera.cs");

//GUI files
exec("art/gui/FadeGui.gui");

%toolsFolder = "tools/worldEditor/";

//Editor GUI and scripts
if (!isObject(CSC_Editor))
    exec(%toolsFolder @ "gui/CSC_Editor.gui");

exec(%toolsFolder @ "scripts/CSC_Editor.cs");
exec(%toolsFolder @ "scripts/CSC_PathEditor.cs");
