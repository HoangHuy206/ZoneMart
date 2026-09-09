import sharp from 'sharp';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function processImage() {
  const inputPath = 'D:\\AnhbaiBaiThayChung_(ZoneMark)\\anhxoanen.jfif';
  const outputPath = path.join(__dirname, 'public', 'images', 'anhxoanen_clean.png');

  console.log('Loading image:', inputPath);
  const image = sharp(inputPath);
  const metadata = await image.metadata();

  console.log(`Original dimensions: ${metadata.width}x${metadata.height}, channels: ${metadata.channels}`);

  // Get raw RGBA buffer
  const { data, info } = await image
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });

  const width = info.width;
  const height = info.height;

  // Process pixels: turn checkerboard background transparent
  for (let i = 0; i < data.length; i += 4) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];

    const isGrayscale = Math.abs(r - g) < 12 && Math.abs(g - b) < 12;
    
    // Background pixels are white (#FFFFFF) or light gray checkerboard pattern (#CCCCCC to #ECECEC)
    const isWhite = r > 245 && g > 245 && b > 245;
    const isCheckerboardGray = isGrayscale && r >= 175 && r <= 244;

    if (isWhite || isCheckerboardGray) {
      data[i + 3] = 0; // Make 100% transparent
    }
  }

  // Write out cleaned image
  await sharp(data, {
    raw: {
      width: width,
      height: height,
      channels: 4
    }
  })
  .trim() // Trim empty transparent borders
  .toFile(outputPath);

  console.log('Cleaned transparent PNG saved to:', outputPath);
}

processImage().catch(console.error);
