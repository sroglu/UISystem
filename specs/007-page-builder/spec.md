# Feature Specification: UISystem Page Builder

**Feature Branch**: `007-page-builder`
**Created**: 2026-04-11
**Status**: Draft
**Input**: WP-10 Editor Tooling extension — visual page composition tool

---

## Overview

UISystem'de 27 M3 component mevcut ancak sayfa oluşturmak hâlâ manual UXML yazımı gerektiriyor. Page Builder, developer'ların Game Tools menüsünden açtığı bir builder scene + inspector window ile sadece M3 componentlerden sayfa compose edip UXML olarak export etmesini sağlar.

Sadece M3 componentler ve layout container'lar kullanılır — M3 standardı dışı element yoktur.

---

## User Scenarios & Testing

### User Story 1 — Page Builder'ı Açma (Priority: P1)

Developer, Unity menü çubuğundan `Game Tools > Page Builder` seçeneğine tıklar. PageBuilderWindow açılır ve sağ tarafa dock olur. Builder scene yüklü değilse "Load Builder Scene" butonu görünür. Scene yüklendiğinde EditorWindow, scene'deki UIDocument'a bağlanır ve boş bir canvas gösterilir.

**Why this priority**: Tool'a erişim olmadan başka hiçbir özellik kullanılamaz.

**Independent Test**:
1. Unity Editor'da `Game Tools > Page Builder` menüsüne tıkla
2. PageBuilderWindow'un açıldığını doğrula
3. "Load Builder Scene" butonuna tıkla
4. Builder scene'in yüklendiğini ve Scene View'da boş canvas'ın göründüğünü doğrula

**Acceptance Scenarios**:
1. **Given** UISystem kurulu, **When** developer `Game Tools > Page Builder` tıklar, **Then** PageBuilderWindow açılır.
2. **Given** builder scene yüklü değil, **When** window açılır, **Then** "Load Builder Scene" butonu görünür.
3. **Given** builder scene yüklenir, **When** UIDocument bulunur, **Then** window status "Connected" olur ve Scene View'da surface-colored boş canvas görünür.

### User Story 2 — M3 Component Ekleme (Priority: P1)

Developer, component palette'te kategorilere göre düzenlenmiş M3 componentleri görür (Actions, Selection, Input, Containment, Communication, Navigation). Bir componente tıklamak onu hierarchy'de seçili container'a (veya root'a) ekler. Component Scene View'da anında görünür.

**Why this priority**: Tool'un temel amacı component eklemektir.

**Independent Test**:
1. Builder bağlıyken palette'ten "M3Button (Filled)" tıkla
2. Scene View'da M3Button'ın göründüğünü doğrula
3. Palette'ten "M3Card (Elevated)" tıkla
4. Scene View'da ikinci componentin de göründüğünü doğrula

**Acceptance Scenarios**:
1. **Given** builder bağlı, **When** developer palette'ten "M3Button (Filled)" tıklar, **Then** Filled variant M3Button root'a eklenir ve Scene View'da görünür.
2. **Given** hierarchy'de bir container seçili, **When** developer bir componente tıklar, **Then** component seçili container'ın child'ı olarak eklenir.
3. **Given** birden fazla component eklenmiş, **When** Scene View'a bakılır, **Then** tüm componentler doğru theme renkleri ve layout ile render edilir.

### User Story 3 — Hierarchy Görünümü ve Yönetimi (Priority: P1)

EditorWindow, sayfadaki tüm elementlerin tree view'ını gösterir. Developer tree'de bir element seçtiğinde Scene View'da highlight olur. Sağ tık menüsü veya Delete tuşu ile element silinebilir.

**Why this priority**: Hierarchy olmadan elementleri yönetmek mümkün değildir.

**Independent Test**:
1. Birkaç component ekle
2. Hierarchy panel'de tree yapısının doğru göründüğünü doğrula
3. Bir elementi seç → Scene View'da highlight olduğunu doğrula
4. Delete tuşuna bas → elementin hem tree'den hem visual tree'den silindiğini doğrula

**Acceptance Scenarios**:
1. **Given** componentler eklenmiş, **When** hierarchy panel'e bakılır, **Then** visual tree yapısını yansıtan bir tree görünür.
2. **Given** tree'de bir element seçili, **When** seçilir, **Then** element Scene View'da highlight olur.
3. **Given** tree'de bir element, **When** developer Delete tuşuna basar, **Then** element hem tree'den hem visual tree'den kaldırılır.

### User Story 4 — Property Inspector (Priority: P2)

Bir element seçildiğinde EditorWindow'un alt bölümünde düzenlenebilir property'ler gösterilir. M3 componentler için `[UxmlAttribute]` property'leri (variant, size, text, disabled), layout container'lar için flex-direction, gap, padding düzenlenebilir. Değişiklikler Scene View'da anında yansır.

**Why this priority**: P1'de componentler default değerlerle eklenebilir; property düzenleme ikinci aşamada gelir.

**Independent Test**:
1. Bir M3Button seç
2. Property inspector'da Variant dropdown'ını "Outlined" olarak değiştir
3. Scene View'da button'ın variant'ının değiştiğini doğrula

**Acceptance Scenarios**:
1. **Given** M3Button seçili, **When** property inspector gösterilir, **Then** Variant, Size, Text ve Disabled property'leri düzenlenebilir.
2. **Given** bir property değiştirilir, **When** değer güncellenir, **Then** Scene View anında yansıtır.
3. **Given** bir layout container seçili, **When** property inspector gösterilir, **Then** flex-direction, justify-content, align-items, padding düzenlenebilir.

### User Story 5 — UXML Export (Priority: P1)

Developer, EditorWindow'da "Export UXML" butonuna tıklar. Save dialog açılır. Tool, visual tree'yi valid bir UXML dosyasına serialize eder. Oluşturulan dosya bir UIDocument'a yüklendiğinde builder'daki ile aynı görsel sonucu verir.

**Why this priority**: Export olmadan builder'da yapılan iş kullanılamaz.

**Independent Test**:
1. 3-4 component ekle
2. "Export UXML" butonuna tıkla
3. Save dialog'da dosya yolunu seç
4. Oluşturulan UXML'i yeni bir UIDocument'a yükle
5. Aynı görsel sonucun elde edildiğini doğrula

**Acceptance Scenarios**:
1. **Given** componentli bir sayfa, **When** "Export UXML" tıklanır, **Then** save file dialog açılır.
2. **Given** dosya kaydedilir, **When** UXML bir UIDocument'a yüklenir, **Then** aynı görsel sonuç render edilir.
3. **Given** custom property'li M3 componentler, **When** export edilir, **Then** tüm non-default `[UxmlAttribute]` değerleri XML attribute olarak serialize edilir.

### User Story 6 — Theme Preview Toggle (Priority: P2)

EditorWindow toolbar'ında light/dark toggle butonu bulunur. Tıklamak builder scene'deki panellerde `ThemeManager.ToggleLightDark()` çağırır ve sayfa her iki temada önizlenebilir.

**Why this priority**: Theme toggle preview için güzel ama core fonksiyonalite değil.

**Independent Test**:
1. Componentli bir sayfa oluştur
2. Theme toggle butonuna tıkla
3. Tüm componentlerin dark/light temaya geçtiğini doğrula

**Acceptance Scenarios**:
1. **Given** componentli bir sayfa, **When** theme toggle tıklanır, **Then** tüm componentler alternatif temaya geçer.

### User Story 7 — UXML Import (Priority: P2)

Developer, mevcut bir UXML dosyasını builder'a yükleyebilir. UXML'deki M3 componentler ve layout yapısı visual tree olarak reconstruct edilir ve düzenlenebilir hale gelir.

**Why this priority**: Paylaşılabilirlik ve iterasyon için önemli ama core'dan sonra gelir.

**Independent Test**:
1. Daha önce export edilmiş bir UXML dosyası seç
2. "Import UXML" butonuna tıkla
3. Builder'da componentlerin doğru şekilde yüklendiğini doğrula
4. Bir property değiştir ve re-export et

**Acceptance Scenarios**:
1. **Given** mevcut bir UXML dosyası, **When** "Import UXML" tıklanır, **Then** visual tree builder'da reconstruct edilir.
2. **Given** import edilmiş bir sayfa, **When** component property'si değiştirilip re-export edilir, **Then** değişiklik yeni UXML'de yansır.

### User Story 8 — Layout Presets (Priority: P3)

Component palette'te bir "Layouts" kategorisi bulunur. Hazır layout şablonları (Button Row, Card Grid, Form Layout, vb.) tek tıkla eklenebilir.

**Why this priority**: Productivty artışı sağlar ama core fonksiyonalite değil.

**Independent Test**:
1. Palette'te "Layouts" kategorisinden "Card Grid" seç
2. 2x2 card grid'in otomatik oluştuğunu doğrula

**Acceptance Scenarios**:
1. **Given** builder bağlı, **When** "Card Grid" preset'i tıklanır, **Then** 2 sütunlu wrap layout ile 4 M3Card oluşturulur.

---

## Edge Cases

- **Builder scene disk'ten silinmiş**: Window hata gösterir ve "Create Builder Scene" butonu sunar.
- **ThemeManager başlatılmamış**: Builder scene ThemeBootstrapper içerir, her zaman çalışır.
- **Export etmeden scene kapatılma**: Uyarı dialog gösterilir.
- **Büyük sayfa (50+ element)**: Hierarchy tree virtualized list kullanır.

---

## Functional Requirements

### Core (P1)

- **FR-001**: Page Builder MUST `Game Tools > Page Builder` menü öğesi ile açılabilir olmalı.
- **FR-002**: Dedicated builder scene `Assets/UISystem/Editor/Scenes/PageBuilder.unity` adresinde MUST bulunmalı.
- **FR-003**: EditorWindow, tüm M3 componentleri kategoriye göre düzenlenmiş bir component palette MUST göstermeli.
- **FR-004**: Palette'te bir öğeye tıklamak `new M3[Component]()` ile component MUST oluşturmalı ve UIDocument'ın visual tree'sine eklemeli.
- **FR-005**: EditorWindow, UIDocument'ın visual tree'sini yansıtan bir hierarchy tree view MUST göstermeli.
- **FR-006**: Hierarchy'de element seçimi, Scene View'da highlight ve EditorWindow'da property inspector (P2) gösterimi MUST sağlamalı.
- **FR-007**: "Export UXML" fonksiyonu, UIDocument tarafından yüklenebilir valid UXML MUST üretmeli.
- **FR-008**: Export edilen UXML, kullanılan componentler için gerekli tüm `<ui:Style>` referanslarını MUST içermeli.
- **FR-009**: Palette'te sadece M3 componentler ve layout container bulunmalı, temel UI Toolkit elementleri (Label, Image, ScrollView vb.) MUST NOT bulunmamalı.

### Polish (P2)

- **FR-010**: Seçili element için `[UxmlAttribute]` property'lerini düzenleyebilen bir property inspector MUST sağlanmalı.
- **FR-011**: Light/dark theme toggle MUST preview sağlamalı.
- **FR-012**: Hierarchy'de drag-and-drop ile element reorder MUST desteklenmeli.
- **FR-013**: Mevcut UXML dosyalarını import edip düzenleyebilme MUST desteklenmeli.
- **FR-014**: "Add Container" komutu ile layout wrapper ekleme MUST desteklenmeli.

### Presets (P3)

- **FR-015**: Component palette, layout presets (Button Row, Card Grid, Form Layout, vb.) içeren "Layouts" kategorisi MUST içermeli.

### Constitution Compliance

- **FR-016**: Tüm kod `mehmetsrl.UISystem.Editor` assembly definition içinde MUST bulunmalı.
- **FR-017**: `mehmetsrl.*` assembly'lerine sıfır cross-reference MUST olmalı.
- **FR-018**: Editor-only tool olmalı, runtime koda değişiklik MUST NOT yapılmamalı.

---

## Success Criteria

- **SC-001**: Developer, Page Builder ile 5+ M3 component içeren bir sayfa 2 dakika altında compose edebilmeli.
- **SC-002**: Export edilen UXML, builder preview'ı ile ayrı bir UIDocument'ta aynı görsel sonucu MUST üretmeli.
- **SC-003**: Sıfır compile error, sıfır `mehmetsrl.*` cross-assembly reference.
- **SC-004**: Builder scene yükleme ve EditorWindow bağlanma 2 saniye altında MUST gerçekleşmeli.
- **SC-005**: Import edilen UXML, builder'da doğru şekilde reconstruct edilmeli ve re-export sonrası aynı sonucu vermeli (P2).

---

## Assumptions

- Unity 6.3+ (6000.3) kullanılır
- UISystem v0.3.0 (006-m3-uisystem-overhaul) tamamlanmış ve main'e merge edilmiş
- ThemeManager, ThemeData ve tüm M3 componentler mevcut ve çalışır durumda
- Editor-only tool — runtime performans etkisi yoktur
- Builder scene editor-only klasörde tutulur, build'e dahil edilmez
