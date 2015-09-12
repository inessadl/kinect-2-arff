using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.IO;

namespace Microsoft.Samples.Kinect.BodyIndexBasics
{
    class KinectFileManager
    {
    ﻿    int _current = 0;
         bool _hasEnumeratedJoints = false;

         Joint[] _lastJoint = new Joint[25]; //create an array to save the last Joint position in x,y, and z axis
         Joint[] _sumJoints = new Joint[25]; //create an array to save the sum Joint position in x,y, and z axis

         public bool IsRecording { get; protected set; }

         public string Folder { get; protected set; }

         public string Result { get; protected set; }

         public void Start()
         {
             Folder = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss");

             Directory.CreateDirectory(Folder);

             IsRecording = true;
         }

         public void Update(Body body)
         {
             if (!IsRecording) return;
             if (body == null || !body.IsTracked) return;

             string path = Path.Combine(Folder, _current.ToString() + ".line");

             using (StreamWriter writer = new StreamWriter(path))
             {
                 StringBuilder line = new StringBuilder();

                 if (!_hasEnumeratedJoints)
                 {
                     //add the headers to a file
                     foreach (var joint in body.Joints.Values)
                     {
                         line.Append(string.Format("{0};;;", joint.JointType.ToString()));
                     }
                     line.AppendLine();

                     //add the headers to a file
                     foreach (var joint in body.Joints.Values)
                     {
                         line.Append("X;Y;Z;");
                     }
                     line.AppendLine();

                     //create an list to save the last joint value in x,y and z axis
                     int i = 0;
                     foreach (var joint in body.Joints.Values)
                     {
                         _lastJoint[i++] = joint;
                     }

                     i = 0;

                     //initialize array with zero in Position values
                     foreach (var joint in _sumJoints)
                     {
                         _sumJoints[i].Position.X = 0.0f;
                         _sumJoints[i].Position.Y = 0.0f;
                         _sumJoints[i++].Position.Z = 0.0f;
                     }


                     _hasEnumeratedJoints = true;
                 }
                 else
                 {
                     int i = 0;

                     //module calculate to Position Joints
                     foreach (var joint in body.Joints.Values)
                     {
                         if (joint.Position.X > _lastJoint[i].Position.X)
                         {
                             _sumJoints[i].Position.X += joint.Position.X - _lastJoint[i].Position.X;
                         }
                         else
                         {
                             _sumJoints[i].Position.X += _lastJoint[i].Position.X - joint.Position.X;
                         }

                         if (joint.Position.Y > _lastJoint[i].Position.Y)
                         {
                             _sumJoints[i].Position.Y += joint.Position.Y - _lastJoint[i].Position.Y;
                         }
                         else
                         {
                             _sumJoints[i].Position.Y += _lastJoint[i].Position.Y - joint.Position.Y;
                         }

                         if (joint.Position.Z > _lastJoint[i].Position.Z)
                         {
                             _sumJoints[i].Position.Z += joint.Position.Z - _lastJoint[i].Position.Z;
                         }
                         else
                         {
                             _sumJoints[i].Position.Z += _lastJoint[i].Position.Z - joint.Position.Z;
                         }
                         i++;
                     }

                     i = 0;

                     //atualiza a articulação considerada a última
                     foreach (var joint in body.Joints.Values)
                     {
                         _lastJoint[i].Position.X = joint.Position.X;
                         _lastJoint[i].Position.Y = joint.Position.Y;
                         _lastJoint[i].Position.Z = joint.Position.Z;
                         i++;
                     }

                     i = 0;

                     foreach (var joint in body.Joints.Values)
                     {
                         //line.Append(string.Format("{0};{1};{2};", joint.Position.X, joint.Position.Y, joint.Position.Z)); --essa linha vai para a função de pausa 
                         line.Append(string.Format("{0};{1};{2};", _sumJoints[i].Position.X, _sumJoints[i].Position.Y, _sumJoints[i].Position.Z));
                     }
                 }
                 writer.Write(line);

                 _current++;
             }
         }

         public void Stop()
         {
             IsRecording = false;
             Result = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss") + ".csv";

             using (StreamWriter writer = new StreamWriter(Result))
             {
                 for (int index = 0; index < _current; index++)
                 {
                     string path = Path.Combine(Folder, index.ToString() + ".line");

                     if (File.Exists(path))
                     {
                         string line = string.Empty;

                         using (StreamReader reader = new StreamReader(path))
                         {
                             line = reader.ReadToEnd();
                         }

                         writer.WriteLine(line);
                     }
                 }
             }
         }

         public void Pause()
         {
             IsRecording = true;
         }

         public void Continue()
         {
             IsRecording = true;
         }

         public void Discard()
         {

         }

    }
}
