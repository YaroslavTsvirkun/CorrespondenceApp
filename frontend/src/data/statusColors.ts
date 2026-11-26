// Кольори тегів статусів документів і доручень (antd Tag color)

export function statusColor(name: string): string {
  switch (name) {
    case 'Зареєстровано': return 'blue';
    case 'На розгляді': return 'gold';
    case 'На виконанні': return 'orange';
    case 'Виконано': return 'green';
    default: return 'default'; // Архівовано та інші
  }
}

export function taskColor(name: string): string {
  switch (name) {
    case 'Призначено': return 'blue';
    case 'У роботі': return 'gold';
    case 'Виконано': return 'green';
    case 'Прострочено': return 'red';
    default: return 'default'; // Скасовано та інші
  }
}
