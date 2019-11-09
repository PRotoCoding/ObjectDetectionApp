using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDetectionApp.Logic
{
    public class DarknetDetectionResult
    {
        public class RelativeCoordinates
        {
            public double center_x;
            public double center_y;
            public double width;
            public double height;
        }

        public class Object
        {
            public int class_id;
            public string name;
            public RelativeCoordinates relative_Coordinates;
            public double confidence;
        }

        public int frame_id;
        public string filename;
        public List<Object> objects;
    }
}
