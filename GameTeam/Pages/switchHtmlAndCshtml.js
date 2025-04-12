// маленький скрипт, который меняет расширения .cshtml в .html, а .html в .cshtml
// (чтобы можно было нормально открывать эти файлы в вебшторме)

const fs = require('fs');
const path = require('path');

// Запускаем функцию
renameFiles();

function renameFiles() {
    const directory = './';

    // Читаем содержимое директории
    fs.readdir(directory, (err, files) => {
        if (err) {
            console.error('Ошибка чтения директории:', err);
            return;
        }

        files.forEach(file => {
            const oldPath = path.join(directory, file);

            // Проверяем, является ли файл .html или .cshtml
            if (file.endsWith('.html')) {
                const newFile = file.replace('.html', '.cshtml');
                const newPath = path.join(directory, newFile);
                renameFile(oldPath, newPath);
            } else if (file.endsWith('.cshtml')) {
                const newFile = file.replace('.cshtml', '.html');
                const newPath = path.join(directory, newFile);
                renameFile(oldPath, newPath);
            }
        });
    });
}

function renameFile(oldPath, newPath) {
    fs.rename(oldPath, newPath, (err) => {
        if (err) {
            console.error(`Ошибка переименования ${oldPath} в ${newPath}:`, err);
        } else {
            console.log(`Файл переименован: ${oldPath} -> ${newPath}`);
        }
    });
}
