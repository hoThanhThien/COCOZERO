package com.cococarbon.app.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

val ForestGreen = Color(0xFF123C29)
val EmeraldGreen = Color(0xFF15803D)
val SoftOffWhite = Color(0xFFF6F8F5)
val MintSurface = Color(0xFFE8F5EC)
val NeutralText = Color(0xFF1B241F)
val MutedText = Color(0xFF526158)

private val CocoColorScheme = lightColorScheme(
    primary = ForestGreen,
    secondary = EmeraldGreen,
    background = SoftOffWhite,
    surface = Color.White,
    onPrimary = Color.White,
    onSecondary = Color.White,
    onBackground = NeutralText,
    onSurface = NeutralText,
    surfaceVariant = MintSurface
)

@Composable
fun CocoCarbonTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = CocoColorScheme,
        content = content
    )
}
