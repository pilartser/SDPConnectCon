using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SDPConnectCon
{
    class IniFile
    {
        readonly string _path; //Имя файла.

        [DllImport("kernel32")] // Подключаем kernel32.dll и описываем его функцию WritePrivateProfilesString
        static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32")] // Еще раз подключаем kernel32.dll, а теперь описываем функцию GetPrivateProfileString
        static extern int GetPrivateProfileString(string section, string key, string Default, StringBuilder retVal, int size, string filePath);

        // С помощью конструктора записываем пусть до файла и его имя.
        public IniFile(string iniPath)
        {
            _path = new FileInfo(iniPath).FullName;
        }

        //Читаем ini-файл и возвращаем значение указного ключа из заданной секции.
        public string ReadIni(string section, string key)
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", retVal, 255, _path);
            return retVal.ToString();
        }
        //Записываем в ini-файл. Запись происходит в выбранную секцию в выбранный ключ.
        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _path);
        }

        //Удаляем ключ из выбранной секции.
        public void DeleteKey(string key, string section = null)
        {
            Write(section, key, null);
        }
        //Удаляем выбранную секцию
        public void DeleteSection(string section = null)
        {
            Write(section, null, null);
        }
        //Проверяем, есть ли такой ключ, в этой секции
        public bool KeyExists(string key, string section = null)
        {
            return ReadIni(section, key).Length > 0;
        }
    }
}
