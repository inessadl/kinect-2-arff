using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.IO;
using System.Globalization;


namespace Microsoft.Samples.Kinect.BodyIndexBasics
{
    class KinectFileManager
    {
         Joint[] _lastJoint = new Joint[25]; //create an array to save the last Joint position in x,y, and z axis
         Joint[] _sumJoints = new Joint[25]; //create an array to save the sum Joint position in x,y, and z axis

         public bool IsRecording { get; protected set; } //Record status

         public string Folder { get; protected set; } //Folder name

         public string Result { get; protected set; } //Final stream/file name

         private bool HasListedJoints { get; set; } //Flag used to writte header file

         private int StreamLineNumber { get; set; } //File line that will set stream buffer

         public void Update(Body body)
         {
             //Changes the system to dot instead comma because WEKA only suports dot in real values
             System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
             customCulture.NumberFormat.NumberDecimalSeparator = ".";
             System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

             if (!IsRecording) return;
             if (body == null || !body.IsTracked) return;

             string path = Path.Combine(Folder, StreamLineNumber.ToString() + ".line");
             int i = 0;
             StreamWriter writer = new StreamWriter(path);
             StringBuilder line = new StringBuilder();
             using (writer)
             {
                 //TODO: change word gesto to moviment name
                 line.Append("@RELATION gesto");
                 line.AppendLine();

                 if (!HasListedJoints)
                 {
                     //add the headers to a file
                     foreach (var joint in body.Joints.Values)
                     {
                         line.Append(string.Format("@ATTRIBUTE {0}X", joint.JointType.ToString()));
                         line.Append(string.Format("  NUMERIC"));
                         line.AppendLine();
                         line.Append(string.Format("@ATTRIBUTE {0}Y", joint.JointType.ToString()));
                         line.Append(string.Format("  NUMERIC"));
                         line.AppendLine();
                         line.Append(string.Format("@ATTRIBUTE {0}Z", joint.JointType.ToString()));
                         line.Append(string.Format("  NUMERIC"));
                         line.AppendLine();
                     }
                     line.Append("@DATA");
                     line.AppendLine();

                     foreach (var joint in body.Joints.Values)
                     {
                         _lastJoint[i++] = joint;
                     }

                     i = 0;

                     //init array with zero in Position values
                     foreach (var joint in _sumJoints)
                     {
                         _sumJoints[i].Position.X = 0.0f;
                         _sumJoints[i].Position.Y = 0.0f;
                         _sumJoints[i++].Position.Z = 0.0f;
                     }


                     HasListedJoints = true;
                     StreamLineNumber++;
                 }
                 else
                 {
                     i = 0;
                     //calculate the Joints Positions module
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

                     //get the new Joints values
                     foreach (var joint in body.Joints.Values)
                     {
                         _lastJoint[i].Position.X = joint.Position.X;
                         _lastJoint[i].Position.Y = joint.Position.Y;
                         _lastJoint[i].Position.Z = joint.Position.Z;
                         i++;
                     }


                 }
                 writer.Write(line); //set the stream with all get values

             }
         }

         //Init all values
         public void Start()
         {
             Folder = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss");
             Directory.CreateDirectory(Folder);
             IsRecording = true;
             HasListedJoints = false;
             StreamLineNumber = 0;
         }

         //Save all stream values in a file
         //while was the index value less than StreamLineNumber
         public void Stop()
         {
             IsRecording = false;
             Result = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss") + ".arff";

             using (StreamWriter writer = new StreamWriter(Result))
             {
                 for (int index = 0; index < StreamLineNumber; index++)
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

         //Add new gesture values in current stream
         //All values corresponding one gesture stay on the same line
         public void Pause()
         {
             IsRecording = true;
             string path = Path.Combine(Folder, StreamLineNumber.ToString() + ".line");
             StreamWriter writer = new StreamWriter(path);
             StringBuilder line = new StringBuilder();
             using (writer)

             {
                 for (int i = 0; i < 25; i++ )
                 {
                     line.Append(string.Format("{0},{1},{2},", _sumJoints[i].Position.X, _sumJoints[i].Position.Y, _sumJoints[i].Position.Z));
                 }
                 StreamLineNumber++;
                 writer.Write(line);
             }
         }

         //Reset all values to get a new gesture
         public void Continue()
         {
             for (int i = 0; i < 25; i++)
             {
                 _sumJoints[i].Position.X = 0.0f;
                 _sumJoints[i].Position.Y = 0.0f;
                 _sumJoints[i].Position.Z = 0.0f;
             }
             IsRecording = true;
          }

         //Reset all values
         public void Discard()
           {
             IsRecording = true;
             for (int i = 0; i < 25; i++)
             {
                 _sumJoints[i].Position.X = 0.0f;
                 _sumJoints[i].Position.Y = 0.0f;
                 _sumJoints[i].Position.Z = 0.0f;
             }
         }

    }
}
