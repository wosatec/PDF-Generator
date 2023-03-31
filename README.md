# Introduction
This project has the goal to read JSON templates and use iText to generate PDF documents from them.

# Usage

```batch
PdfGenerator.exe ^
    --template ".\Sample Data\template.json" ^
    --pages ".\Sample Data\page.json" ^
    --data ".\Sample Data\data.json" ^
    --output ".\output\test.pdf"
```

The following files can be edited.

## template.json
Contains basic information about the layout of the document overall.
- colors
  - `default` Default Font Color (mandatory)
  - `default-background` Default Background Color (mandatory)
  - more colors can have any name and be referenced later
- fonts
  - `default` Default Font (mandatory)
  - `default-icon` Default Icon Font - will be automatically used when a token has a Unicode value above `0xF000` (mandatory)
  - more fonts can have any name and be referenced later
- header
  - `startsAtPage` sets at which page the header should start to be displayed (helpful when you have a cover page)
  - contains elements that are displayed in the header
- footer
  - `startsAtPage` sets at which page the footer should start to be displayed (helpful when you have a cover page)
  - contains elements that are displayed in the footer
- draft (experimental)
  - defines displayed elements that mark a document as a draft

## page.json
Contains layout information for repeated pages.
- `contentKey` the name of the array in `data.json` that contains the page content
- `margin` the page margins (in mm)
- `elements` contains the elements that are displayed for each data set of the page

## data.json
This is the dynamic content for the document. Could be generated by our external application.
- `documentContents` an array of images (mandatory)

## Element

```json
{
    type: 1, // can be a value from 1-6, see types
    position: {
        top: 0, // Position from top in mm
        left: 0 // Position from left in mm
    },
    width: 0,   // Width in mm
    height: 0   // Height in mm
}
```

more properties depend on the selected type.

Most types will conain a `contentKey` which reads the content by the provided key from `data.json`.

### Types

#### 1 = Text Line
Prints all text tokens next to each other, delimitted by a space.

Content can be a string or an array of strings.

#### 2 = Text Block
Prints all text tokens under each other, delimitted by a new line character.

Content can be a string or an array of strings.

#### 3 = Image
Displays an image.

Content must be a DataImage with

```json
{
    type: "base64",             // can be "base64", "id" or "path"
    size: "",                   // can be "small" or "medium"; only supported for type "id"
    content: "<some base64>"    // contains what makes the image content depending on the type
}
```

##### Base64
Directly contains the exact image to display.
Useful for small images that appear once.

##### Id
Contains the Id that the documentContents array in `data.json` contains.
Useful for reusable images to keep the `data.json` small.

Only works in conjunction with the `size` property.

##### Path
Contains a path that contains the file to include on the document.
Useful for shared placeholders that could be held in the `Resouces` directory.

#### 4 = Table
Draws a table.

Contains definitions for headers and row layouts.

#### 5 = Line
Draws a horizontal line.

#### 6 = AreaBreak
Renders a page break and creates a new page.

# Build und Test
The solution can be loaded and built in Visual Studio (tested in 2022 Professional).
The contained `launchSettings.json` launches the console application with the provided sample data
and generates a simple PDF file.
