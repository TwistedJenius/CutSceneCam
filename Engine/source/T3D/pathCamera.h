//-----------------------------------------------------------------------------
// Copyright (c) 2012 GarageGames, LLC
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

#ifndef _PATHCAMERA_H_
#define _PATHCAMERA_H_

#ifndef _SHAPEBASE_H_
#include "T3D/shapeBase.h"
#endif

#ifndef _CAMERASPLINE_H_
#include "T3D/cameraSpline.h"
#endif


//----------------------------------------------------------------------------
struct PathCameraData: public ShapeBaseData {
   typedef ShapeBaseData Parent;

   // ************************** Cut Scene Cam: Start
  public:
   // camera shake data
   VectorF           camShakeFreq;
   VectorF           camShakeAmp;
   F32               camShakeFalloff;

   PathCameraData();
   // ************************** Cut Scene Cam: End

   //
   DECLARE_CONOBJECT(PathCameraData);
   static void consoleInit();
   static void initPersistFields();
   virtual void packData(BitStream* stream);
   virtual void unpackData(BitStream* stream);
};


//----------------------------------------------------------------------------
class PathCamera: public ShapeBase
{
public:
   enum State {
      Forward,
      Backward,
      Stop,
      StateBits = 3
   };

   // ************************** Cut Scene Cam: Start
   enum HeadingMode {
      Direction,
      Ahead,
      Object,
      Location,
      HeadingModeBits = 3
   };
   // ************************** Cut Scene Cam: End

private:
   typedef ShapeBase Parent;

   enum MaskBits {
      WindowMask     = Parent::NextFreeMask,
      PositionMask   = Parent::NextFreeMask + 1,
      TargetMask     = Parent::NextFreeMask + 2,
      StateMask      = Parent::NextFreeMask + 3,
	  // ************************** Cut Scene Cam: Start
	  HeadingMask    = Parent::NextFreeMask + 4,
	  AheadMask      = Parent::NextFreeMask + 5,
	  // ************************** Cut Scene Cam: End
      NextFreeMask   = Parent::NextFreeMask << 1
   };

   struct StateDelta {
      F32 time;
      F32 timeVec;
   };
   StateDelta delta;

   enum Constants {
      NodeWindow = 128    // Maximum number of active nodes
   };

   //
   PathCameraData* mDataBlock;
   CameraSpline mSpline;
   S32 mNodeBase;
   S32 mNodeCount;
   F32 mPosition;
   S32 mState;
   F32 mTarget;
   bool mTargetSet;

   // ************************** Cut Scene Cam: Start
   Point3F mHeadingPos;
   F32 mHeadingRot;
   U32 mHeadingObj;
   Point3F mHeadingOffset;
   S32 mHeadingMode;
   Point3F mHeadingAhead;
   Point3F mHeadingDiff;
   U32 mHeadingAxis;
   // ************************** Cut Scene Cam: End

   void interpolateMat(F32 pos,MatrixF* mat);
   void advancePosition(S32 ms);
   // ************************** Cut Scene Cam: Start
   void setRenderTransform(const MatrixF& mat);
   void _setRenderPosition(const Point3F& pos,const Point3F& rot);
   void applyHeading(Point3F target, MatrixF* mat, CameraSpline::Knot knot);
   // ************************** Cut Scene Cam: End

public:
   DECLARE_CONOBJECT(PathCamera);
   
   DECLARE_CALLBACK( void, onNode, (S32 node));

   PathCamera();
   ~PathCamera();
   static void initPersistFields();
   static void consoleInit();

   void onEditorEnable();
   void onEditorDisable();

   bool onAdd();
   void onRemove();
   bool onNewDataBlock( GameBaseData *dptr, bool reload );
   void onNode(S32 node);

   void processTick(const Move*);
   void interpolateTick(F32 dt);
   void getCameraTransform(F32* pos,MatrixF* mat);

   U32  packUpdate(NetConnection *, U32 mask, BitStream *stream);
   void unpackUpdate(NetConnection *, BitStream *stream);

   void reset(F32 speed = 1);
   void pushFront(CameraSpline::Knot *knot);
   void pushBack(CameraSpline::Knot *knot);
   void popFront();

   void setPosition(F32 pos);
   void setTarget(F32 pos);
   void setState(State s);

   // ************************** Cut Scene Cam: Start
   void setHeadingPos(Point3F pos, bool mode);
   void setHeadingRot(F32 rot, U32 axis, bool mode);
   void setHeadingObj(S32 targetObject, Point3F offset, bool mode);
   void setHeadingMode(HeadingMode r);
   void resetHeading();
   void setCameraShake(F32 duration);
   int getHeadingMode() const { return mHeadingMode; }
   int getState() const { return mState; }
   // ************************** Cut Scene Cam: End
};


#endif
