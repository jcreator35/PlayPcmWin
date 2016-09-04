using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;

namespace WWXmlRW {
    public interface SaveLoadContents {
        int GetVersionNumber();
        int GetCurrentVersionNumber();

    }

    /// <summary>
    /// TをXML形式でセーブロードする
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XmlRW<T> where T : class, SaveLoadContents, new() {

        private readonly string m_fileName;
        private bool m_useIsolatedStorage;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="fileName">useIsolatedStorage==falseの場合、フルパスでファイルを指定。useIsolatedStorage==trueの場合、フォルダ名を含まないファイル名。</param>
        /// <param name="useIsolatedStorage">true: IsolatedStorageに保存する。false:通常のフォルダに保存。</param>
        public XmlRW(string fileName, bool useIsolatedStorage = true) {
            m_fileName = fileName;
            m_useIsolatedStorage = useIsolatedStorage;
        }

        public bool Load(out T t) {
            bool result = false;
            T p = new T();

            try {
                if (m_useIsolatedStorage) {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(
                            m_fileName, System.IO.FileMode.Open,
                            IsolatedStorageFile.GetUserStoreForDomain())) {
                        byte[] buffer = new byte[isfs.Length];
                        isfs.Read(buffer, 0, (int)isfs.Length);
                        System.IO.MemoryStream stream = new System.IO.MemoryStream(buffer);
                        XmlSerializer formatter = new XmlSerializer(typeof(T));
                        p = formatter.Deserialize(stream) as T;
                        isfs.Close();
                        result = true;
                    }
                } else {
                    using (Stream fs = new FileStream(
                            m_fileName, System.IO.FileMode.Open)) {
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, (int)fs.Length);
                        System.IO.MemoryStream stream = new System.IO.MemoryStream(buffer);
                        XmlSerializer formatter = new XmlSerializer(typeof(T));
                        p = formatter.Deserialize(stream) as T;
                        fs.Close();
                        result = true;
                    }
                }
            } catch (System.Exception ex) {
                Console.WriteLine(ex);
                p = new T();
            }

            if (p.GetCurrentVersionNumber() != p.GetVersionNumber()) {
                Console.WriteLine("Version mismatch {0} != {1}", p.GetCurrentVersionNumber(), p.GetVersionNumber());
                p = new T();
                result = false;
            }

            t = p;
            return result;
        }

        public T Load() {
            T t;
            Load(out t);
            return t;
        }

        public bool Save(T p) {
            bool result = false;

            try {
                if (m_useIsolatedStorage) {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(
                            m_fileName, System.IO.FileMode.Create,
                            IsolatedStorageFile.GetUserStoreForDomain())) {
                        XmlSerializer s = new XmlSerializer(typeof(T));
                        s.Serialize(isfs, p);
                        result = true;
                    }
                } else {
                    using (FileStream fs = new FileStream(
                            m_fileName, System.IO.FileMode.Create)) {
                        XmlSerializer s = new XmlSerializer(typeof(T));
                        s.Serialize(fs, p);
                        result = true;
                    }
                }
            } catch (System.Exception ex) {
                Console.WriteLine(ex);
            }

            return result;
        }
    }
}
