# PCB Analysis Tool

This project is a tool for analyzing printed circuit boards (PCBs) to detect manufacturing defects using the [EmguCV](https://www.emgu.com/) library. The program identifies missing holes and mouse bites on PCBs and allows graphical representation or file export of the detected defects.

## Features
- **Missing Hole Detection:** Identifies missing drill holes on the PCB.
- **Mouse Bite Detection:** Detects small irregularities on the contour lines of the PCB.
- **Visualization Options:** Display results, save to file, or analyze defects against annotations.

## Usage

### PCB Class
#### Constructor
Initializes the class with an image and an annotation XML file.

#### Methods
1. **RunMissingHole**
   - Detects missing holes.
   - **Parameters:**
     - `threshold`: Threshold value for binarization.
     - `areaMin` (optional): Minimum area size.
     - `areaMax` (optional): Maximum area size.
     - `roundnessLimit`: Roundness threshold (0 to 1).

2. **RunMouseBite**
   - Detects mouse bite defects.
   - **Parameters:**
     - `snakeLength`: Length of the contour section to examine.
     - `checkLength`: Length of the segments for validation.
     - `minHeight`: Minimum deviation from the line.
     - `maxHeight` (optional): Maximum deviation from the line.

#### Parameters
- `Path`: Path to the defective image.
- `Annotation`: Path to the annotation file.
- `Mode`: Defines the operation mode (Show, Save, Analyze).

#### Modes
- **Show:** Displays the results.
- **Save:** Saves the results to a file.
- **Analyze:** Compares defects with annotations.

## Implementation Details

### Missing Hole Detection
The process begins by converting the image to grayscale and applying Gaussian smoothing to reduce noise. It then performs thresholding and dilation to enhance the relevant features. Contours are detected and filtered based on their roundness and area size. Finally, duplicate detections are prevented by maintaining a list of saved positions.

### Mouse Bite Detection
The process involves similar preprocessing steps to those used in missing hole detection. It evaluates contour segments to identify irregularities and validates the results by analyzing segment deviation and peak prominence.

### Functions
- `CreateFolderIfNotExists`: Ensures a folder exists or creates it.
- `GetAnnotationData`: Retrieves defect positions from the annotation file.
- `DistanceFromLine`: Calculates the distance of a point from a line.

## Results

### Missing Hole Result
![Missing Hole PCB](https://i.imgur.com/pQoDVoT.jpeg)

### Mouse Bite Result
![Mouse Bite PCB](https://i.imgur.com/9eTwpGG.jpeg)

## References
- [EmguCV Documentation](https://emgu.com/wiki/files/3.3.0/document/)
- [OpenCV Eroding and Dilating](https://docs.opencv.org/3.4/db/df6/tutorial_erosion_dilatation.html)
- [C# API Documentation](https://learn.microsoft.com/en-us/dotnet/api/)
- [Distance from a Point to a Line](https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line)
- [PCB Defects Dataset](https://www.kaggle.com/datasets/akhatova/pcb-defects)
