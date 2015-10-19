# KinectToArff

Gets Kinect body data (joint positions) and exports to Attribute-Relation File Format. 

.arff files are used with [Weka Machine Learning Software](http://www.cs.waikato.ac.nz/ml/weka/).

--

- **Coordinate system**

	- The origin (x = 0, y = 0, z = 0) is located at the center of the IR sensor on Kinect
	- X grows to the sensor’s left
	- Y grows up
	- Z grows out in the direction the sensor is facing
	- 1 unit = 1 meter

![Coordinate mapping](https://i-msdn.sec.s-msft.com/dynimg/IC757720.png) 

This code is based on [Kinect2CSV](https://github.com/LightBuzz/Kinect-2-CSV) from **LightBuzz**.

See also: [Kinect Coordinate Mapping](https://msdn.microsoft.com/en-us/library/dn785530.aspx)
