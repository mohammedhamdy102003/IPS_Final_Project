# Windows Traffic Agent

سكربت بيشتغل على جهاز Windows (الجهاز اللي عايز تحميه)، بيستخدم `nfstream` عشان
يلتقط الـ network flows الحقيقية ويبعتها لـ API بتاع الـ IPS عشان تتصنّف بالـ AI model.

## قبل ما تشغله

1. **لازم Npcap متركب على جهاز الويندوز** (مكتبة التقاط الباكتات):
   https://npcap.com/#download

2. **عدّل سطرين في `windows_traffic_agent.py`:**

   ```python
   API_URL = "http://<EC2_PUBLIC_IP>:30500/api/Traffic/ProcessTraffic"
   INTERFACE = r"\Device\NPF_{...}"   # GUID بتاع كارت الشبكة بتاع جهازك إنت، مش جهاز تاني
   ```

   عشان تعرف الـ GUID بتاع كارت الشبكة بتاعك، شغّل في PowerShell:
   ```powershell
   Get-NetAdapter | Select-Object Name, InterfaceGuid
   ```

3. **تركيب المكتبات:**
   ```bash
   pip install nfstream requests
   ```

## تشغيله

```bash
python windows_traffic_agent.py
```

السكربت هيفضل شغال بيلتقط الترافيك الحقيقي من جهازك ويبعته للـ backend live.
