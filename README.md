# ae.exe

openssl�R�}���h�݊��̃t�@�C���Í��\�t�g�B  
Linux��Windows�ԂňÍ����t�@�C���𑊌݂ɂ����\�ł��B  

## �g����

.NET Framework 4.6�ȍ~���K�v�B�iWindows10�͕W���ŃC���X�g�[���ρj

```
ae.exe [options] <input file>

options
  -h,--help               �w���v��\��
  -v,--version            �o�[�W������\��
  -e,--encrypt            �Í�������(�f�t�H���g)
  -d,--decrypt            ��������
  -k,--password <string>  �p�X���[�h���w��
  -o,--out <string>       �o�̓t�@�C�������w��
  --aes-256-cbc           keysize=256,mode=cbc��aes�𗘗p
  --aes-192-cbc           keysize=192,mode=cbc��aes�𗘗p
  --aes-128-cbc           keysize=128,mode=cbc��aes�𗘗p
```

�f�t�H���g�̓���� `-e --aes-256-cbc` �ł��B�܂� `-o` `--out` �ŏo�̓t�@�C�������w�肵�Ȃ������ꍇ�͏o�̓t�@�C�������ȉ��̃��[���ŕt�^���܂��B

- �Í������F���t�@�C�����Ɋg���q.ae��t��
- �������F���t�@�C���Ɋg���q.ae�������.ae�������B.ae���Ȃ��ꍇ��.decoded��t���B

### ���p��

aaa.txt���Í����B�iaaa.txt.ae���o�́j

    ae.exe aaa.txt

aaa.txt.ae�𕜍��B�iaaa.txt���o�́j

    ae.exe -d aaa.txt.ae

Linux��openssl�R�}���h�œ������Ƃ�����ɂ͈ȉ��̂悤�ɂȂ�B

```
�Í���
openssl aes-256-cbc -in aaa.txt -out aaa.txt.ae

������
openssl enc -d -aes-256-cbc -in aaa.txt.ae -out aaa.txt

```


## �Ή�����Í�����

- aes-128-cbc
- aes-192-cbc
- aes-256-cbc (�f�t�H���g)

�K�v�ɉ����đ��₷�\��B

### �Q�l�F�Í��t�@�C���t�H�[�}�b�g

openssl�R�}���h����������t�@�C���ƌ݊��B

| offset | bytes | data               |
|--------|-------|--------------------|
| +0     | 8     | ������ "Salted__"  |
| +8     | 8     | Salt               |
| +16    | -     | �Í������ꂽ�f�[�^ |

Salt�Ȃ��̏ꍇ�͐擪16�o�C�g���Ȃ��Ȃ�B


## ���C�Z���X

[MIT](https://opensource.org/licenses/MIT)
