import sharp from 'sharp';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function processImagePrecise() {
  const inputPath = 'D:\\AnhbaiBaiThayChung_(ZoneMark)\\anhxoanen.jfif';
  const outputPath = path.join(__dirname, 'public', 'images', 'anhxoanen_clean.png');

  console.log('Processing image precisely:', inputPath);
  const image = sharp(inputPath);
  const metadata = await image.metadata();

  const { data, info } = await image
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });

  const width = info.width;
  const height = info.height;

  let minX = width, maxX = 0, minY = height, maxY = 0;

  // Step 1: Detect background pixels vs actual illustration artwork pixels
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const idx = (y * width + x) * 4;
      const r = data[idx];
      const g = data[idx + 1];
      const b = data[idx + 2];

      const maxRGB = Math.max(r, g, b);
      const minRGB = Math.min(r, g, b);
      const chroma = maxRGB - minRGB;

      // Background pixels in this image are light cream / off-white paper texture (#F7F4EE)
      // or light gray / white checkerboard squares.
      // Artwork elements have dark outlines (low RGB values), vibrant green/red/orange/blue colors (high chroma), or dark text.
      const isDarkLine = maxRGB < 185; // ink / outline / shadow lines
      const isColor = chroma > 18; // colored fruits, veggies, logo, wheels
      const isMediumDark = maxRGB < 210 && chroma > 8;

      const isArtworkPixel = isDarkLine || isColor || isMediumDark;

      if (isArtworkPixel) {
        // Keep artwork pixel
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
        if (y < minY) minY = y;
        if (y > maxY) maxY = y;
      } else {
        // Remove background pixel (100% transparent)
        data[idx + 3] = 0;
      }
    }
  }

  console.log(`Detected Artwork Bounding Box: X[${minX}..${maxX}], Y[${minY}..${maxY}]`);

  // Step 2: Create sharp pipeline with raw buffer, crop to bounding box
  const cropWidth = maxX - minX + 1;
  const cropHeight = maxY - minY + 1;

  await sharp(data, {
    raw: {
      width: width,
      height: height,
      channels: 4
    }
  })
  .extract({ left: minX, top: minY, width: cropWidth, height: cropHeight })
  .png()
  .toFile(outputPath);

  console.log(`Successfully saved cropped transparent image (${cropWidth}x${cropHeight}) to: ${outputPath}`);
}

processImagePrecise().catch(console.error);
